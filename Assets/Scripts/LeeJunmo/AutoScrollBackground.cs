using UnityEngine;

public class AutoScrollBackground : MonoBehaviour
{
    [Tooltip("속도 정보를 가져올 TrainController를 여기에 연결해주세요.")]
    public TrainController trainController;

    [Tooltip("카메라를 기준으로 배경 위치를 재설정합니다.")]
    public Transform cameraTransform;

    [Tooltip("관리할 배경 레이어들을 등록해주세요.")]
    public ParallaxLayer[] layers;

    private float spriteWidth;

    void Start()
    {
        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        if (trainController == null)
        {
            Debug.LogError("TrainController가 연결되지 않았습니다! 인스펙터에서 연결해주세요.", this.gameObject);
            this.enabled = false;
        }
    }

    void Update()
    {
        if (trainController == null) return;

        // TrainController로부터 현재 속력을 실시간으로 받아옵니다.
        float currentTrainSpeed = trainController.CurrentSpeed;

        // 각 레이어를 패럴랙스 요소에 맞게 이동시킵니다.
        foreach (ParallaxLayer layer in layers)
        {
            float movement = currentTrainSpeed * layer.parallaxFactor * Time.deltaTime;
            layer.layerTransform.position -= new Vector3(movement, 0, 0);
        }

        // 배경 이미지가 화면 밖으로 나가면 반대편으로 옮겨서 무한 스크롤 효과를 줍니다.
        foreach (ParallaxLayer layer in layers)
        {
            if (layer.layerTransform.childCount == 0) continue;

            spriteWidth = layer.layerTransform.GetChild(0).GetComponent<SpriteRenderer>().bounds.size.x;

            if (currentTrainSpeed > 0 && cameraTransform.position.x - layer.layerTransform.GetChild(0).position.x >= spriteWidth)
            {
                Transform leftChild = layer.layerTransform.GetChild(0);
                Transform rightChild = layer.layerTransform.GetChild(1);
                leftChild.position = new Vector3(rightChild.position.x + spriteWidth, leftChild.position.y, leftChild.position.z);
                leftChild.SetAsLastSibling();
            }
            else if (currentTrainSpeed < 0 && cameraTransform.position.x - layer.layerTransform.GetChild(1).position.x <= -spriteWidth)
            {
                Transform leftChild = layer.layerTransform.GetChild(0);
                Transform rightChild = layer.layerTransform.GetChild(1);
                rightChild.position = new Vector3(leftChild.position.x - spriteWidth, rightChild.position.y, rightChild.position.z);
                rightChild.SetAsFirstSibling();
            }
        }
    }
}