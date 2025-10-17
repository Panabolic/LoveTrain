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
            return;
        }
    }

    void Update()
    {
        if (trainController == null) return;

        // ✨ 이제 TrainController.CurrentSpeed가 정상적으로 작동하므로 이 코드는 유효합니다.
        float currentTrainSpeed = trainController.CurrentSpeed;

        // 각 레이어를 패럴랙스 요소에 맞게 이동시킵니다.
        foreach (ParallaxLayer layer in layers)
        {
            // 속도가 0이면 움직이지 않으므로 계산할 필요가 없습니다.
            if (Mathf.Approximately(currentTrainSpeed, 0)) continue;

            float movement = currentTrainSpeed * layer.parallaxFactor * Time.deltaTime;
            layer.layerTransform.position -= new Vector3(movement, 0, 0);
        }

        // 배경 이미지가 화면 밖으로 나가면 반대편으로 옮겨서 무한 스크롤 효과를 줍니다.
        // 이 로직은 currentTrainSpeed를 기반으로 하므로 수정할 필요가 없습니다.
        foreach (ParallaxLayer layer in layers)
        {
            if (layer.layerTransform.childCount < 2) continue; // 자식 오브젝트가 2개 미만이면 무한 스크롤 불가

            // spriteWidth는 한 번만 계산해도 되지만, 유연성을 위해 Update에 둡니다.
            spriteWidth = layer.layerTransform.GetChild(0).GetComponent<SpriteRenderer>().bounds.size.x;

            Transform leftChild = layer.layerTransform.GetChild(0);
            Transform rightChild = layer.layerTransform.GetChild(1);

            // 카메라와 이미지의 상대 위치를 계산하여 재배치 여부를 결정합니다.
            if (currentTrainSpeed > 0 && cameraTransform.position.x > rightChild.position.x)
            {
                leftChild.position = new Vector3(rightChild.position.x + spriteWidth, leftChild.position.y, leftChild.position.z);
                leftChild.SetAsLastSibling();
            }
            else if (currentTrainSpeed < 0 && cameraTransform.position.x < leftChild.position.x)
            {
                rightChild.position = new Vector3(leftChild.position.x - spriteWidth, rightChild.position.y, rightChild.position.z);
                rightChild.SetAsFirstSibling();
            }
        }
    }
}