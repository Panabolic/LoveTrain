using UnityEngine;

public class Enemy : MonoBehaviour
{
    protected SpriteRenderer  sprite;
    protected Rigidbody2D     rigid2D;

    protected float hp;
    protected bool isAlive;

    protected Rigidbody2D targetRigid;
    protected Vector2 moveDirection;

    [SerializeField] protected float moveSpeed;


    private void Awake()
    {
        sprite  = GetComponent<SpriteRenderer>();
        rigid2D = GetComponent<Rigidbody2D>();
        targetRigid = GameObject.FindWithTag("Player").GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        moveDirection = targetRigid.position - rigid2D.position;
        Vector2 nextXPosition = new Vector2(Mathf.Sign(moveDirection.x) * moveSpeed, rigid2D.position.y) * Time.fixedDeltaTime;
        rigid2D.MovePosition(rigid2D.position + nextXPosition);
        rigid2D.linearVelocity = Vector2.zero;
    }
}
