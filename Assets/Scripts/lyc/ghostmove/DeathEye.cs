using UnityEngine;
using UnityEngine.UI;
using TMPro;
[RequireComponent(typeof(RawImage))]
public class DeathEye : MonoBehaviour
{
    public DeathMeter meter;

    [Header("動畫")]
    public float openSpeed = 3f;      // 越小睜開得越慢
    public float shakeStart = 0.8f;   // 超過這個比例開始抖
    public float shakeAmount = 4f;    // 抖動幅度（像素）

    [Header("線條")]
    public Color lineColor = new Color(0.95f, 0.95f, 0.95f);   // 一開始的顏色
    public Color dangerColor = new Color(0.85f, 0.1f, 0.1f);   // 快死時的顏色
    public float thickness = 1.5f;  
    [Header("百分比")]
    public TMP_Text percentText;                                  // 線條半寬（像素）

    const int W = 256;
    const int H = 128;
    const int Steps = 100;            // 睜眼分成幾段，每跨一段才重畫

    static readonly float[] lashU = { -0.6f, -0.3f, 0f, 0.3f, 0.6f };   // 睫毛位置
    readonly Vector2[] lashA = new Vector2[5];
    readonly Vector2[] lashB = new Vector2[5];

    Texture2D tex;
    Color32[] pixels;
    RectTransform rt;
    Vector2 basePos;
    float open;                       // 目前的睜開程度 0～1
    int drawnStep = -1;               // 上次畫的是第幾段

    void Awake()
    {
        tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        pixels = new Color32[W * H];

        GetComponent<RawImage>().texture = tex;
        rt = GetComponent<RectTransform>();
        basePos = rt.anchoredPosition;

        drawnStep = 0;
        Draw(0f);
        UpdateText(0);
    }

    void Update()
    {
        float target = meter != null ? meter.Percent : 0f;
        open = Mathf.Lerp(open, target, 1f - Mathf.Exp(-openSpeed * Time.deltaTime));

        // 跨到下一段才重畫
        int step = Mathf.RoundToInt(open * Steps);
        if (step != drawnStep)
        {
            drawnStep = step;
            Draw(step / (float)Steps);
            UpdateText(step);
        }

        // 快死時抖動
        if (open > shakeStart)
        {
            float k = (open - shakeStart) / (1f - shakeStart);
            rt.anchoredPosition = basePos + Random.insideUnitCircle * shakeAmount * k;
        }
        else
        {
            rt.anchoredPosition = basePos;
        }
    }

    void Draw(float o)
    {
        Color col = Color.Lerp(lineColor, dangerColor, o * o);
        float half = H * 0.5f;
        float cx = W * 0.5f;
        float cy = H * 0.5f;

        float irisR = H * 0.30f;
        float pupilRx = H * Mathf.Lerp(0.02f, 0.08f, o);
        float pupilRy = H * 0.20f;

        // 睫毛：f 從 -1（朝下）變到 1（朝上），中間縮到 0
        float f = Mathf.SmoothStep(0f, 1f, o / 0.3f) * 2f - 1f;
        float lashLen = Mathf.Lerp(7f, 14f, o) * Mathf.Abs(f);
        bool showLashes = Mathf.Abs(f) > 0.05f;

        for (int i = 0; i < lashU.Length; i++)
        {
            float uk = lashU[i];
            float s = 1f - uk * uk;
            Vector2 a = new Vector2((uk + 1f) * 0.5f * W, cy + o * s * 0.9f * half);

            // 沿著上眼皮的垂直方向長出去，再往兩側張開一點
            float slope = -o * 0.9f * half * 2f * uk * 2f / W;
            Vector2 n = new Vector2(-slope * f, f).normalized;
            n = (n + new Vector2(uk * 0.6f, 0f)).normalized;

            lashA[i] = a;
            lashB[i] = a + n * lashLen;
        }

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float u = px / W * 2f - 1f;
                float s = 1f - u * u;
                float d = float.MaxValue;   // 這個像素到最近線條的距離

                if (s > 0f)
                {
                    float upper = cy + o * s * 0.9f * half;
                    float lower = cy - o * s * 0.7f * half;

                    // 上下眼皮
                    d = Mathf.Min(Mathf.Abs(py - upper), Mathf.Abs(py - lower));

                    // 虹膜和瞳孔只畫在眼皮裡面
                    if (py > lower && py < upper)
                    {
                        float dx = px - cx;
                        float dy = py - cy;
                        d = Mathf.Min(d, Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - irisR));

                        if ((dx / pupilRx) * (dx / pupilRx) + (dy / pupilRy) * (dy / pupilRy) < 1f)
                            d = 0f;   // 瞳孔是實心
                    }
                }

                if (showLashes)
                {
                    for (int i = 0; i < lashA.Length; i++)
                        d = Mathf.Min(d, SegmentDistance(new Vector2(px, py), lashA[i], lashB[i]));
                }

                // 越靠近線條越不透明，邊緣自然柔化
                float alpha = Mathf.Clamp01(thickness + 0.5f - d);
                pixels[y * W + x] = new Color(col.r, col.g, col.b, col.a * alpha);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false);
    }
    void UpdateText(int step)
    {
        if (percentText == null) return;

        float o = step / (float)Steps;
        percentText.text = step + "%";
        percentText.color = Color.Lerp(lineColor, dangerColor, o * o);   // 和線條同色
    }
    static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    void OnDestroy()
    {
        if (tex != null) Destroy(tex);
    }
}