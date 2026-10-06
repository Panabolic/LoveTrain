using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DG.Tweening;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Copied project only. Frozen world renders through production URP and real transition tweens.
[InitializeOnLoad]
public static class TunnelCameraValidation
{
    const BindingFlags Members=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    const int Width=1280,Height=720;
    static string output;
    static bool running;
    static int warmup,checks;
    static double deadline;
    static Camera camera;
    static RenderTexture target;
    static StageManager stage;
    static Train train;
    static ForwardCameraFollow follow;
    static GameManager game;
    static Vector3 originalRig, originalTrain;
    static RenderPipelineAsset quality,defaults;
    static readonly Dictionary<Sprite,Bounds> alphaCache=new Dictionary<Sprite,Bounds>();
    static readonly List<UnityEngine.Object> temporary=new List<UnityEngine.Object>();

    static TunnelCameraValidation(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../tunnel-camera-validation"));Directory.CreateDirectory(output);
        try
        {
            string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            Require(Application.productName.StartsWith("DriveBossPreview_") && project.Replace('\\','/').EndsWith("/outputs/drive-boss-validation/project",StringComparison.OrdinalIgnoreCase),"Isolated copied-project identity verified");
            Require(!EditorApplication.isPlaying,"Editor method probes only");
            File.WriteAllText(Result,"START: production URP tunnel scene-point/sequence validation. No source saves, PlayMode, player build, wall-clock timing or full stage cycle claim.\n");
            quality=QualitySettings.renderPipeline;defaults=GraphicsSettings.defaultRenderPipeline;
            var pipeline=(quality!=null?quality:defaults) as UniversalRenderPipelineAsset;
            Require(pipeline!=null,"Effective production pipeline is URP");
            Require(pipeline.scriptableRenderer!=null && pipeline.scriptableRenderer.GetType().Name=="Renderer2D","Effective production renderer is Renderer2D");
            EditorSceneManager.OpenScene("Assets/Scenes/Junmo.unity",OpenSceneMode.Single);
            stage=Find<StageManager>();train=Get<Train>(stage,"train");follow=Get<ForwardCameraFollow>(stage,"cameraFollow");game=Find<GameManager>();camera=Camera.main;
            Require(stage!=null && train!=null && follow!=null && camera!=null,"Actual StageManager/train/follow/MainCamera references resolve");
            foreach(Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))canvas.gameObject.SetActive(false);
            foreach(MonoBehaviour component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(component!=null && component.GetType().Assembly==typeof(Train).Assembly)component.enabled=false;
            foreach(Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))renderer.enabled=false;
            foreach(Renderer renderer in train.GetComponentsInChildren<Renderer>(true))renderer.enabled=true;
            foreach(Camera other in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))other.enabled=other==camera;
            camera.rect=new Rect(0,0,1,1);camera.aspect=(float)Width/Height;
            Require(Mathf.Abs(camera.orthographicSize-20.8f)<.001f,"Camera retains authored1.3x orthographic20.8");
            originalRig=follow.transform.position;originalTrain=train.transform.position;Set(follow,"initialX",originalRig.x);
            SetStatic(typeof(GameManager),"<Instance>k__BackingField",game);SetStatic(typeof(StageManager),"Instance",stage);
            DOTween.Init(false,true,LogBehaviour.ErrorsOnly);
            target=new RenderTexture(Width,Height,24,RenderTextureFormat.ARGB32){antiAliasing=1,hideFlags=HideFlags.HideAndDontSave};target.Create();temporary.Add(target);camera.targetTexture=target;
            deadline=EditorApplication.timeSinceStartup+60d;running=true;EditorApplication.QueuePlayerLoopUpdate();
        }
        catch(Exception ex){Finish(false,ex.ToString());}
    }
    static void Tick()
    {
        if(!running)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Tunnel render fixture deadline");
            if(++warmup<5){EditorApplication.QueuePlayerLoopUpdate();return;}
            running=false;
            var settings=Get<List<StageManager.StageTransitionSetting>>(stage,"transitionSettings");Require(settings.Count==2,"Two authored transition settings");
            foreach(float offset in new[]{0f,100f,1000f})for(int index=0;index<settings.Count;index++)Probe(index,offset,settings[index]);
            Require(QualitySettings.renderPipeline==quality && GraphicsSettings.defaultRenderPipeline==defaults,"Production pipeline assets retained");
            Finish(true,"PASS "+checks+" actual scene-reference, DOTween milestone, offset geometry and production URP Renderer2D render checks.");
        }
        catch(Exception ex){Finish(false,ex.ToString());}
    }
    static void Probe(int index,float offset,StageManager.StageTransitionSetting setting)
    {
        CleanupCase();follow.transform.position=originalRig+Vector3.right*offset;train.transform.position=originalTrain+Vector3.right*offset;
        Set(game,"<CurrentState>k__BackingField",GameState.Playing);Time.timeScale=1f;
        Call(stage,"LoadStage",index);
        GameObject background=Get<GameObject>(stage,"currentStageObject");Require(background!=null,"Actual stage background instantiated with authored bgParent/root and camera offset");
        foreach(MonoBehaviour component in background.GetComponentsInChildren<MonoBehaviour>(true))component.enabled=false;
        var scroll=background.GetComponent<AutoScrollBackground>();scroll.cameraTransform=camera.transform;Call(scroll,"EnsureViewportCoverage");
        stage.StartStageTransitionSequence();Sequence sequence=Get<Sequence>(stage,"transitionSequence");GameObject tunnel=Get<GameObject>(stage,"transitionTunnel");
        Require(sequence!=null && tunnel!=null,"Real StartStageTransitionSequence creates tween and tunnel");
        sequence.Pause();sequence.ForceInit();
        Vector3 shift=Vector3.right*offset;
        Require(Vector3.Distance(tunnel.transform.position,setting.tunnelSpawnPoint.position+shift)<.001f,"Spawn uses authored point plus camera-follow offset");
        Inspect(index,offset,"spawn",tunnel);Capture(index,offset,"spawn");
        sequence.Goto(6f,false);
        Require(Vector3.Distance(tunnel.transform.position,setting.tunnelTargetPoint.position+shift)<.001f,"Actual tween arrives at authored target at6sec");
        Vector3 reset=Get<Vector3>(stage,"playerResetPosition")+shift;
        Require(Vector3.Distance(train.transform.position,reset)<.001f,"Train remains at reset position when tunnel arrives");
        Inspect(index,offset,"arrive",tunnel);Capture(index,offset,"arrive");
        sequence.Goto(7.5f,false);
        Require(Vector3.Distance(train.transform.position,setting.trainEnterPoint.position+shift)<.001f,"Actual tween reaches authored train entry at7.5sec");
        Bounds trainBounds=RendererBounds(train.gameObject);float cameraRight=camera.transform.position.x+camera.orthographicSize*camera.aspect;
        Require(trainBounds.min.x>cameraRight,"Train visual fully clears right camera edge on entry");
        Inspect(index,offset,"enter",tunnel);Capture(index,offset,"enter");CleanupCase();
    }
    static void Inspect(int index,float offset,string phase,GameObject tunnel)
    {
        float left=float.PositiveInfinity,right=float.NegativeInfinity,opaqueLeft=float.PositiveInfinity,opaqueRight=float.NegativeInfinity;
        foreach(SpriteRenderer renderer in tunnel.GetComponentsInChildren<SpriteRenderer>(true))
        {
            left=Mathf.Min(left,renderer.bounds.min.x);right=Mathf.Max(right,renderer.bounds.max.x);
            Bounds alpha=Alpha(renderer.sprite);float min=renderer.transform.TransformPoint(alpha.min).x,max=renderer.transform.TransformPoint(alpha.max).x;opaqueLeft=Mathf.Min(opaqueLeft,min);opaqueRight=Mathf.Max(opaqueRight,max);
            Write("SPRITE tunnel"+(index+1)+" "+phase+" offset="+offset+" "+renderer.name+" sorting="+renderer.sortingLayerName+"/"+renderer.sortingOrder+" material="+renderer.sharedMaterial.name+" bounds="+renderer.bounds);
        }
        float edge=camera.transform.position.x+camera.orthographicSize*camera.aspect;
        float screenLeft=camera.WorldToScreenPoint(new Vector3(opaqueLeft,0,0)).x,screenRight=camera.WorldToScreenPoint(new Vector3(opaqueRight,0,0)).x;
        Write("GEOMETRY tunnel"+(index+1)+" "+phase+" offset="+offset+" cameraRight="+edge+" rendererEdges="+left+".."+right+" alphaEdges="+opaqueLeft+".."+opaqueRight+" screenAlpha="+screenLeft+".."+screenRight);
        if(phase=="spawn")Require(opaqueLeft>edge,"Opaque tunnel spawn remains offscreen right");
        else Require(Mathf.Abs(right-edge)<.15f,"Arrived tunnel renderer right remains aligned to expanded camera edge");
        if(phase=="arrive")Require(screenRight>Width-3 && screenRight<Width+3,"Actual opaque art right edge meets viewport edge within3px");
    }
    static Bounds Alpha(Sprite sprite)
    {
        if(alphaCache.TryGetValue(sprite,out Bounds cached))return cached;
        string path=AssetDatabase.GetAssetPath(sprite);var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(Path.GetFullPath(path)));Color32[] colors=texture.GetPixels32();Rect rect=sprite.rect;
        int minx=(int)rect.xMax,miny=(int)rect.yMax,maxx=-1,maxy=-1;
        for(int y=(int)rect.yMin;y<(int)rect.yMax;y++)for(int x=(int)rect.xMin;x<(int)rect.xMax;x++)if(colors[y*texture.width+x].a>0){minx=Mathf.Min(minx,x);miny=Mathf.Min(miny,y);maxx=Mathf.Max(maxx,x);maxy=Mathf.Max(maxy,y);}
        UnityEngine.Object.DestroyImmediate(texture);Require(maxx>=0,"Tunnel source PNG contains opaque pixels");
        Vector3 min=new Vector3((minx-rect.xMin-sprite.pivot.x)/sprite.pixelsPerUnit,(miny-rect.yMin-sprite.pivot.y)/sprite.pixelsPerUnit,0),max=new Vector3((maxx+1-rect.xMin-sprite.pivot.x)/sprite.pixelsPerUnit,(maxy+1-rect.yMin-sprite.pivot.y)/sprite.pixelsPerUnit,0);
        cached=new Bounds((min+max)*.5f,max-min);alphaCache[sprite]=cached;return cached;
    }
    static Bounds RendererBounds(GameObject root){var all=root.GetComponentsInChildren<SpriteRenderer>(true);Bounds bounds=all[0].bounds;foreach(var renderer in all)bounds.Encapsulate(renderer.bounds);return bounds;}
    static void Capture(int index,float offset,string phase)
    {
        var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};RenderPipeline.SubmitRenderRequest(camera,request);
        Require(RenderPipeline.SupportsRenderRequest(camera,request) && RenderPipelineManager.currentPipeline is UniversalRenderPipeline,"Production URP SingleCameraRequest executes");
        if(offset!=0)return;
        RenderTexture prior=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(Width,Height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,Width,Height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,"tunnel"+(index+1)+"-"+phase+".png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=prior;
    }
    static void CleanupCase()
    {
        var sequence=Get<Sequence>(stage,"transitionSequence");sequence?.Kill(false);Set(stage,"transitionSequence",null);
        foreach(string field in new[]{"transitionTunnel","currentStageObject"}){var go=Get<GameObject>(stage,field);Set(stage,field,null);if(go!=null)UnityEngine.Object.DestroyImmediate(go);}
    }
    static void Finish(bool ok,string message)
    {
        running=false;if(string.IsNullOrEmpty(output))return;Write((ok?"PASS ":"FAIL ")+message);
        if(stage!=null)CleanupCase();if(camera!=null)camera.targetTexture=null;foreach(UnityEngine.Object obj in temporary)if(obj!=null)UnityEngine.Object.DestroyImmediate(obj);EditorApplication.Exit(ok?0:1);
    }
    static T Find<T>()where T:Component{foreach(T item in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(item.gameObject.scene.IsValid())return item;throw new Exception("Missing "+typeof(T));}
    static T Get<T>(object owner,string field)=>(T)owner.GetType().GetField(field,Members).GetValue(owner);
    static void Set(object owner,string field,object value)=>owner.GetType().GetField(field,Members).SetValue(owner,value);
    static void SetStatic(Type type,string field,object value)=>type.GetField(field,Members).SetValue(null,value);
    static object Call(object owner,string method,params object[] args)=>owner.GetType().GetMethod(method,Members).Invoke(owner,args);
    static string Result=>Path.Combine(output,"result.txt");
    static void Write(string value)=>File.AppendAllText(Result,value+"\n");
    static void Require(bool valid,string message){if(!valid)throw new InvalidOperationException(message);checks++;if(!string.IsNullOrEmpty(output))Write("CHECK "+message);}
}
