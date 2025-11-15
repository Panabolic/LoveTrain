using UnityEngine;
using System; // System.Serializable

[Serializable] // 인스펙터에 보이도록
public class EventResultOutput
{
    // 텍스트 출력 순서를 정하는 열거형(enum)
    public enum OutputOrder
    {
        DefaultFirst, // 기본 텍스트 -> 특수 텍스트 순
        SpecialFirst  // 특수 텍스트 -> 기본 텍스트 순
    }

    [Tooltip("이 효과의 '기본' 텍스트(예: '아이템 획득...')를 출력합니다.")]
    public bool includeDefaultText = true;

    [Tooltip("별도로 출력할 '특수' 텍스트입니다. (비워두면 무시)")]
    [TextArea]
    public string specialText; // 사용자가 직접 입력할 특수 텍스트

    [Tooltip("기본 텍스트와 특수 텍스트의 출력 순서를 정합니다.")]
    public OutputOrder order = OutputOrder.DefaultFirst;
}