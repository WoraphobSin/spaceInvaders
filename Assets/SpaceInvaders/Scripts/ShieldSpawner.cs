using UnityEngine;

// Builds the green shields out of many small ShieldBlocks, using a text "drawing" of the shape.
// '#' = a block, '.' = empty. Try editing the shape in the Inspector!
public class ShieldSpawner : MonoBehaviour
{
    public ShieldBlock blockPrefab;
    public int shieldCount = 4;
    public float spacingX = 3.5f;
    public float blockSize = 0.1875f;
    public string[] shape =
    {
        "...######...",
        ".##########.",
        "############",
        "############",
        "############",
        "###......###",
        "##........##",
    };

    public void Build()
    {
        foreach (Transform child in transform) Destroy(child.gameObject);

        int rows = shape.Length;
        for (int s = 0; s < shieldCount; s++)
        {
            float centerX = (s - (shieldCount - 1) / 2f) * spacingX;
            for (int r = 0; r < rows; r++)
            {
                string line = shape[r];
                for (int c = 0; c < line.Length; c++)
                {
                    if (line[c] != '#') continue;
                    ShieldBlock block = Instantiate(blockPrefab, transform);
                    block.transform.localPosition = new Vector3(
                        centerX + (c - (line.Length - 1) / 2f) * blockSize,
                        ((rows - 1) / 2f - r) * blockSize,
                        0f);
                }
            }
        }
    }
}
