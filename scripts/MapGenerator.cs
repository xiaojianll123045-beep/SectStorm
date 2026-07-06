using Godot;

public static class MapGenerator
{
    public static ImageTexture GenerateTerrain(int width, int height, int seed)
    {
        // 纯色地形，用小纹理缩放即可
        var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        image.SetPixel(0, 0, Color.FromHtml("#c8c0a8"));
        return ImageTexture.CreateFromImage(image);
    }
}
