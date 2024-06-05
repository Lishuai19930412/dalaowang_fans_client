using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UI_LibItem : MonoBehaviour
{
    [SerializeField] Toggle m_Tog_Expand;
    [SerializeField] Image m_Img_Expand;
    [SerializeField] Text m_Txt_ItemName;

    [SerializeField] RectTransform m_Rt_BtnContent;
    [SerializeField] Button m_Btn_Pbf;

    private float m_HeightRT = 0;
    private List<Button> m_Btn_List = new List<Button>();

    public float HeightRT
    {
        get
        {
            return m_HeightRT;
        }
    }
    public UnityAction<bool> onExpand;
    public UnityAction<string, string> onTexChoose;

    private void Awake()
    {
        m_Tog_Expand.onValueChanged.AddListener((b) =>
        {
            if (b == true)
            {
                m_Img_Expand.rectTransform.localScale = new Vector3(1, -1, 1);
                m_Rt_BtnContent.gameObject.SetActive(true);
                m_HeightRT = ((RectTransform)transform).sizeDelta.y + m_Rt_BtnContent.sizeDelta.y;
                onExpand?.Invoke(true);
            }
            else
            {
                m_Img_Expand.rectTransform.localScale = new Vector3(1, 1, 1);
                m_Rt_BtnContent.gameObject.SetActive(false);
                m_HeightRT = ((RectTransform)transform).sizeDelta.y;
                onExpand?.Invoke(false);
            }
        });
    }

    public void Init(string itemName, List<string> texName)
    {
        m_Tog_Expand.SetIsOnWithoutNotify(false);
        m_Img_Expand.rectTransform.localScale = Vector3.one;
        m_Txt_ItemName.text = itemName;
        m_Btn_Pbf.gameObject.SetActive(false);

        for (int i = 0; i < texName.Count; i++)
        {
            int temp = i;
            if (i < m_Btn_List.Count)
            {
                m_Btn_List[i].onClick.RemoveAllListeners();
                m_Btn_List[i].onClick.AddListener(() => { onTexChoose?.Invoke(itemName, texName[temp]); });
                m_Btn_List[i].GetComponentInChildren<Text>().text = texName[i];
                ((RectTransform)m_Btn_List[i].transform).anchoredPosition = new Vector2(0, -5 - (i * 25));
            }
            else
            {
                Button btn = Instantiate(m_Btn_Pbf, m_Rt_BtnContent);
                btn.onClick.AddListener(() => { onTexChoose?.Invoke(itemName, texName[temp]); });
                btn.gameObject.SetActive(true);
                RectTransform rt_btn = (RectTransform)btn.transform;
                rt_btn.GetComponentInChildren<Text>().text = texName[i];
                rt_btn.anchoredPosition = new Vector2(0, -5 - (i * 25));
                m_Btn_List.Add(btn);
            }
        }

        if (m_Btn_List.Count > texName.Count)
        {
            for (int i = texName.Count; i < m_Btn_List.Count; i++)
            {
                Destroy(m_Btn_List[i].gameObject);
            }
            m_Btn_List.RemoveRange(texName.Count, m_Btn_List.Count - texName.Count);
        }

        m_Rt_BtnContent.sizeDelta = new Vector2(165, texName.Count * 25);
        m_Rt_BtnContent.anchoredPosition = new Vector2(15, 0);
        m_Rt_BtnContent.gameObject.SetActive(false);
        m_HeightRT = ((RectTransform)transform).sizeDelta.y;
    }
}
