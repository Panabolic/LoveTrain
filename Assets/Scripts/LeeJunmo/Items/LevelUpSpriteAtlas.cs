// LevelSpriteAtlas.cs
using UnityEngine;

// "Create" 메뉴에서 이 에셋을 만들 수 있게 해줍니다.
[CreateAssetMenu(fileName = "LevelSpriteAtlas", menuName = "Inventory/Level Sprite Atlas")]
public class LevelSpriteAtlas : ScriptableObject
{
    [Header("레벨 1, 2, 3... 순서대로 스프라이트 등록")]
    // 0번 인덱스 = 1레벨(I), 1번 인덱스 = 2레벨(II) ...
    public Sprite[] levelSprites;

    [Header("최대 레벨")]
    [Tooltip("최대 레벨(MAX)일 때 표시할 스프라이트")]
    public Sprite maxLevelSprite;

    /// <summary>
    /// 레벨(int)을 받아서 스프라이트(Sprite)를 반환하는 헬퍼 함수
    /// </summary>
    public Sprite GetSpriteForLevel(int level)
    {
        int index = level - 1;
        if (levelSprites != null && index >= 0 && index < levelSprites.Length)
        {
            return levelSprites[index];
        }
        return null;
    }
}