using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Drawing;

public class ST_Lib
{
    public string name;
    public List<string> texName = new List<string>();
    public List<string> texPath = new List<string>();
}

public class GlobalMgr : MonoBehaviour
{
    private static string m_Url = "https://codeload.github.com/Lishuai19930412/dalaowang_fans_warehouse/zip/refs/heads/main";
    private static string m_BasePath = "dalaowang_fans_warehouse-main";

    [SerializeField] UI_LibItem m_LibItem_Pbf;
    [SerializeField] RectTransform m_Rt_LibItemCont;
    [SerializeField] RectTransform m_Rt_PreviewSize;
    [SerializeField] RawImage m_Rmg_Preview;
    [SerializeField] Button m_Btn_OriSize;
    [SerializeField] Button m_Btn_Adaptive;
    [SerializeField] Button m_Btn_CheckUpdate;
    [SerializeField] Button m_Btn_Copy;

    [SerializeField] RectTransform m_Rt_CopyTips;
    [SerializeField] RectTransform m_Rt_Mask;
    [SerializeField] Text m_Txt_Tips;

    private float m_TipsTimer;
    private string m_TexPathRT;
    private Texture2D m_TexRT;
    private Dictionary<string, ST_Lib> m_Dict_Lib = new Dictionary<string, ST_Lib>();
    private List<UI_LibItem> m_LibItems = new List<UI_LibItem>();
    private List<ST_Lib> m_Libs = new List<ST_Lib>();
    

    private void Awake()
    {
        m_Rt_CopyTips.gameObject.SetActive(false);
        m_LibItem_Pbf.gameObject.SetActive(false);
        m_Btn_OriSize.gameObject.SetActive(true);
        m_Btn_Adaptive.gameObject.SetActive(false);

        m_Btn_OriSize.onClick.AddListener(() => { Preview_OriSize(); });
        m_Btn_Adaptive.onClick.AddListener(() => { Preview_Adaptive(); });
        m_Btn_Copy.onClick.AddListener(() =>
        {
            if(string.IsNullOrEmpty(m_TexPathRT) == false && File.Exists(m_TexPathRT) == true)
            {
                System.Drawing.Image sysImg = Bitmap.FromFile(m_TexPathRT);
                if (sysImg != null)
                {
                    System.Windows.Forms.Clipboard.SetImage(sysImg);
                    m_Rt_CopyTips.gameObject.SetActive(true);
                    m_Rt_CopyTips.GetComponentInChildren<Text>().text = "已复制到剪贴板";
                    m_TipsTimer = 0;
                }
            }
        });

        m_Btn_CheckUpdate.onClick.AddListener(() =>
        {
            m_Rt_Mask.gameObject.SetActive(true);
            m_Txt_Tips.text = "正在检查更新...";
            StartCoroutine(DownloadAndExtract());
        });

    }
    private void Start()
    {
        if (Directory.Exists(Path.Combine(Application.persistentDataPath, m_BasePath)) == false)
        {
            Debug.Log("首次运行，直接检查更新");
            m_Rt_Mask.gameObject.SetActive(true);
            m_Txt_Tips.text = "正在检查更新...";
            StartCoroutine(DownloadAndExtract());
        }
        else
        {
            ReadData();
        }
    }
    private void Update()
    {
        if(m_Rt_CopyTips.gameObject.activeSelf == true && m_TipsTimer < 1.5f)
        {
            m_TipsTimer += Time.deltaTime;

            if(m_TipsTimer >= 1.5f)
            {
                m_Rt_CopyTips.gameObject.SetActive(false);
            }
        }
    }

    private void ReadData()
    {
        Debug.Log("扫描目录......");

        DirectoryInfo dif = new DirectoryInfo(Path.Combine(Application.persistentDataPath, m_BasePath));
        DirectoryInfo[] difs = dif.GetDirectories("*", SearchOption.TopDirectoryOnly);
        m_Libs.Clear();
        m_Dict_Lib.Clear();

        for (int i = 0; i < difs.Length; i++)
        {
            ST_Lib lb = new ST_Lib();
            lb.name = difs[i].Name;

            List<FileInfo> fis = new List<FileInfo>(50);
            fis.AddRange(difs[i].GetFiles("*.jpg", SearchOption.TopDirectoryOnly));
            fis.AddRange(difs[i].GetFiles("*.png", SearchOption.TopDirectoryOnly));

            lb.texName = new List<string>(fis.Count);
            lb.texPath = new List<string>(fis.Count);

            for (int j = 0; j < fis.Count; j++)
            {
                lb.texName.Add(Path.GetFileNameWithoutExtension(fis[j].Name));
                lb.texPath.Add(fis[j].FullName);
            }

            m_Libs.Add(lb);
            m_Dict_Lib.Add(lb.name, lb);
        }
        UpdateLibItems();
        LibItemLayout();

        Debug.Log("刷新完成");
    }

    private void UpdateLibItems()
    {
        for(int i = 0; i < m_Libs.Count; i++)
        {
            if (i < m_LibItems.Count)
            {
                m_LibItems[i].Init(m_Libs[i].name, m_Libs[i].texName);
                m_LibItems[i].onTexChoose = (libName, texName) => { LoadPreview(libName, texName); };
            }
            else
            {
                UI_LibItem libIt = Instantiate(m_LibItem_Pbf, m_Rt_LibItemCont);
                libIt.onExpand += (b) => { LibItemLayout(); };
                libIt.onTexChoose = (libName, texName) => { LoadPreview(libName, texName); };
                libIt.gameObject.SetActive(true);
                libIt.Init(m_Libs[i].name, m_Libs[i].texName);
                m_LibItems.Add(libIt);
            }
        }

        if (m_LibItems.Count > m_Libs.Count)
        {
            for (int i = m_Libs.Count; i < m_LibItems.Count; i++)
            {
                Destroy(m_LibItems[i].gameObject);
            }
            m_LibItems.RemoveRange(m_Libs.Count, m_LibItems.Count - m_Libs.Count);
        }
    }

    private void LibItemLayout()
    {
        float y = -5;

        for (int i = 0; i < m_LibItems.Count; i++)
        {
            ((RectTransform)m_LibItems[i].transform).anchoredPosition = new Vector2(0, y);
            y -= (m_LibItems[i].HeightRT + 5);
        }

        m_Rt_LibItemCont.sizeDelta = new Vector2(m_Rt_LibItemCont.sizeDelta.x, -y);
    }

    private void LoadPreview(string libName, string texName)
    {
        void SetPreviewEmpty()
        {
            m_Rmg_Preview.rectTransform.localPosition = Vector3.zero;
            m_Rmg_Preview.rectTransform.sizeDelta = new Vector2(300, 300);
            m_Rmg_Preview.color = new Color32(15, 15, 15, 255);
            m_Rmg_Preview.texture = null;
            m_TexPathRT = "";
            if (m_TexRT != null)
            {
                Destroy(m_TexRT);
            }
        }

        SetPreviewEmpty();

        if (m_Dict_Lib.ContainsKey(libName) == true)
        {
            int id = m_Dict_Lib[libName].texName.IndexOf(texName);

            if(id >= 0)
            {
                m_TexPathRT = m_Dict_Lib[libName].texPath[id];
                Texture2D tex = new Texture2D(2, 2);
                bool res = tex.LoadImage(File.ReadAllBytes(m_TexPathRT));

                if(res == true)
                {
                    m_TexRT = tex;
                    m_Rmg_Preview.texture = tex;
                    m_Rmg_Preview.color = UnityEngine.Color.white;
                    m_Rmg_Preview.rectTransform.sizeDelta = new Vector2(tex.width, tex.height);
                    m_Rmg_Preview.rectTransform.localPosition = Vector3.zero;
                    Preview_Adaptive();
                }
            }
        }
    }

    private void Preview_OriSize()
    {
        m_Rmg_Preview.rectTransform.localScale = Vector3.one;
        m_Btn_OriSize.gameObject.SetActive(false);
        m_Btn_Adaptive.gameObject.SetActive(true);
    }

    private void Preview_Adaptive()
    {
        if (m_Rmg_Preview.texture != null)
        {
            float cont_wh = m_Rt_PreviewSize.rect.width / (float)m_Rt_PreviewSize.rect.height;          //预览图框的宽高比
            float tex_wh = m_Rmg_Preview.texture.width / (float)m_Rmg_Preview.texture.height;           //图片的宽高比

            if (tex_wh >= cont_wh)//适应宽度
            {
                float sc = Mathf.Clamp(m_Rt_PreviewSize.rect.width / (float)m_Rmg_Preview.texture.width, 0, 1);
                m_Rmg_Preview.rectTransform.localScale = new Vector3(sc, sc, 1);
            }
            else//适应高度
            {
                float sc = Mathf.Clamp(m_Rt_PreviewSize.rect.height / (float)m_Rmg_Preview.texture.height, 0, 1);
                m_Rmg_Preview.rectTransform.localScale = new Vector3(sc, sc, 1);
            }
        }

        m_Btn_OriSize.gameObject.SetActive(true);
        m_Btn_Adaptive.gameObject.SetActive(false);
    }

    private IEnumerator DownloadAndExtract()
    {
        string zipFilePath = Path.Combine(Application.persistentDataPath, "dalaowang_fans_warehouse-main.zip");

        if (File.Exists(zipFilePath) == true)
        {
            File.Delete(zipFilePath);
        }

        UnityWebRequest uwr = UnityWebRequest.Get(m_Url);
        uwr.downloadHandler = new DownloadHandlerFile(zipFilePath);
        yield return uwr.SendWebRequest();

        if (uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("下载zip失败: " + uwr.error);
            m_Txt_Tips.text = "更新失败";
            StartCoroutine(DelayToDo(2, () => { m_Rt_Mask.gameObject.SetActive(false); }));
        }
        else
        {
            Debug.Log("下载ZIP成功: " + zipFilePath);

            try
            {
                if (Directory.Exists(Path.Combine(Application.persistentDataPath, m_BasePath)) == true)
                {
                    Directory.Delete(Path.Combine(Application.persistentDataPath, m_BasePath), true);
                }
                ZipFile.ExtractToDirectory(zipFilePath, Application.persistentDataPath, true);
                File.Delete(zipFilePath);
                ReadData();
                m_Txt_Tips.text = "更新成功";
                Debug.Log("更新成功");
                StartCoroutine(DelayToDo(2, () => { m_Rt_Mask.gameObject.SetActive(false);}));
            }
            catch (Exception ex)
            {
                Debug.LogError("解压失败: " + ex);
                m_Txt_Tips.text = "更新失败";
                StartCoroutine(DelayToDo(2, () => { m_Rt_Mask.gameObject.SetActive(false); }));
            }
        }
    }  

    private IEnumerator DelayToDo(float delay, UnityAction call)
    {
        yield return new WaitForSeconds(delay);
        call?.Invoke();
    }
}
