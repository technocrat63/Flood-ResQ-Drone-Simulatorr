using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class TrainingDataCapture : MonoBehaviour
{
    public Transform[] people;           // Drag all 5 Person objects here
    public int totalImages = 300;        // How many training images to generate
    public float captureInterval = 0.4f; // Seconds between captures

    private int imageCount = 0;
    private string saveFolder;
    private Camera cam;
    private const int texSize = 640;

    void Start()
    {
        cam = GetComponent<Camera>();
        saveFolder = Path.Combine(Application.dataPath, "../TrainingData");
        if (!Directory.Exists(saveFolder))
            Directory.CreateDirectory(saveFolder);

        StartCoroutine(CaptureLoop());
    }

    IEnumerator CaptureLoop()
    {
        while (imageCount < totalImages)
        {
            RandomizeCameraPosition();
            yield return new WaitForEndOfFrame();
            CaptureAndLabel();
            imageCount++;
            yield return new WaitForSeconds(captureInterval);
        }
        Debug.Log("Done! Captured " + imageCount + " training images in: " + saveFolder);
    }

    void RandomizeCameraPosition()
    {
        Transform target = people[Random.Range(0, people.Length)];

        float distance = Random.Range(8f, 18f);
        float angle = Random.Range(0f, 360f);
        float height = Random.Range(8f, 20f);

        Vector3 offset = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
            height,
            Mathf.Sin(angle * Mathf.Deg2Rad) * distance
        );

        transform.position = target.position + offset;

        Vector3 lookTarget = target.position + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
        transform.LookAt(lookTarget);
    }

    void CaptureAndLabel()
    {
        RenderTexture rt = new RenderTexture(texSize, texSize, 24);
        cam.targetTexture = rt;
        RenderTexture.active = rt;
        cam.Render();

        Texture2D screenShot = new Texture2D(texSize, texSize, TextureFormat.RGB24, false);
        screenShot.ReadPixels(new Rect(0, 0, texSize, texSize), 0, 0);
        screenShot.Apply();

        string baseName = "image_" + imageCount.ToString("D4");
        File.WriteAllBytes(Path.Combine(saveFolder, baseName + ".png"), screenShot.EncodeToPNG());

        List<string> labels = new List<string>();

        foreach (Transform person in people)
        {
            Renderer[] renderers = person.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) continue;

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
                bounds.Encapsulate(r.bounds);

            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;
            Vector3[] corners = new Vector3[8]
            {
                c + new Vector3( e.x,  e.y,  e.z), c + new Vector3( e.x,  e.y, -e.z),
                c + new Vector3( e.x, -e.y,  e.z), c + new Vector3( e.x, -e.y, -e.z),
                c + new Vector3(-e.x,  e.y,  e.z), c + new Vector3(-e.x,  e.y, -e.z),
                c + new Vector3(-e.x, -e.y,  e.z), c + new Vector3(-e.x, -e.y, -e.z)
            };

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            bool anyInFront = false;

            foreach (Vector3 corner in corners)
            {
                Vector3 sp = cam.WorldToScreenPoint(corner);
                if (sp.z > 0) anyInFront = true;
                minX = Mathf.Min(minX, sp.x);
                maxX = Mathf.Max(maxX, sp.x);
                minY = Mathf.Min(minY, sp.y);
                maxY = Mathf.Max(maxY, sp.y);
            }

            if (!anyInFront) continue;

            minX = Mathf.Clamp(minX, 0, texSize);
            maxX = Mathf.Clamp(maxX, 0, texSize);
            minY = Mathf.Clamp(minY, 0, texSize);
            maxY = Mathf.Clamp(maxY, 0, texSize);

            float boxW = maxX - minX;
            float boxH = maxY - minY;
            if (boxW < 5 || boxH < 5) continue;

            float centerX = (minX + maxX) / 2f / texSize;
            float centerY = 1f - ((minY + maxY) / 2f / texSize);
            float normW = boxW / texSize;
            float normH = boxH / texSize;

            labels.Add($"0 {centerX:F6} {centerY:F6} {normW:F6} {normH:F6}");
        }

        File.WriteAllLines(Path.Combine(saveFolder, baseName + ".txt"), labels);

        cam.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
        Destroy(screenShot);
    }
}