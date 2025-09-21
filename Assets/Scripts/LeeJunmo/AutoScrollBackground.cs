using UnityEngine;

public class AutoScrollBackground : MonoBehaviour
{
    [Tooltip("�ӵ� ������ ������ TrainController�� ���⿡ �������ּ���.")]
    public TrainController trainController;

    [Tooltip("ī�޶� �������� ��� ��ġ�� �缳���մϴ�.")]
    public Transform cameraTransform;

    [Tooltip("������ ��� ���̾���� ������ּ���.")]
    public ParallaxLayer[] layers;

    private float spriteWidth;

    void Start()
    {
        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        if (trainController == null)
        {
            Debug.LogError("TrainController�� ������� �ʾҽ��ϴ�! �ν����Ϳ��� �������ּ���.", this.gameObject);
            this.enabled = false;
        }
    }

    void Update()
    {
        if (trainController == null) return;

        // TrainController�κ��� ���� �ӷ��� �ǽð����� �޾ƿɴϴ�.
        float currentTrainSpeed = trainController.CurrentSpeed;

        // �� ���̾ �з����� ��ҿ� �°� �̵���ŵ�ϴ�.
        foreach (ParallaxLayer layer in layers)
        {
            float movement = currentTrainSpeed * layer.parallaxFactor * Time.deltaTime;
            layer.layerTransform.position -= new Vector3(movement, 0, 0);
        }

        // ��� �̹����� ȭ�� ������ ������ �ݴ������� �Űܼ� ���� ��ũ�� ȿ���� �ݴϴ�.
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