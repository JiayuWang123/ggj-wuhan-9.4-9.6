using System.Collections.Generic;
using UnityEngine;

public static class SpriteRevealBounds
{
    public struct Bounds2D
    {
        public float MinX;
        public float MaxX;
        public float MinY;
        public float MaxY;

        public float CenterX => (MinX + MaxX) * 0.5f;
        public float CenterY => (MinY + MaxY) * 0.5f;
        public float HalfHeight => (MaxY - MinY) * 0.5f;
    }

    public enum Source
    {
        FullSpriteBounds,
        AlphaPixels,
        PhysicsShape
    }

    public static bool TryComputeTightBounds(
        Sprite sprite,
        float alphaCutoff,
        out Bounds2D bounds,
        out Source source)
    {
        bounds = default;
        source = Source.FullSpriteBounds;

        if (sprite == null)
        {
            return false;
        }

        if (TryGetAlphaTightBounds(sprite, alphaCutoff, out bounds))
        {
            source = Source.AlphaPixels;
            return true;
        }

        if (TryGetPhysicsShapeBounds(sprite, out bounds))
        {
            source = Source.PhysicsShape;
            return true;
        }

        Bounds fullBounds = sprite.bounds;
        bounds = new Bounds2D
        {
            MinX = fullBounds.min.x,
            MaxX = fullBounds.max.x,
            MinY = fullBounds.min.y,
            MaxY = fullBounds.max.y
        };
        return true;
    }

    private static bool TryGetAlphaTightBounds(Sprite sprite, float alphaCutoff, out Bounds2D bounds)
    {
        bounds = default;

        Texture2D texture = sprite.texture;
        if (texture == null || !texture.isReadable)
        {
            return false;
        }

        Rect rect = sprite.rect;
        int minPixelX = int.MaxValue;
        int maxPixelX = int.MinValue;
        int minPixelY = int.MaxValue;
        int maxPixelY = int.MinValue;
        bool foundOpaquePixel = false;

        int startX = Mathf.FloorToInt(rect.xMin);
        int endX = Mathf.CeilToInt(rect.xMax);
        int startY = Mathf.FloorToInt(rect.yMin);
        int endY = Mathf.CeilToInt(rect.yMax);

        for (int pixelY = startY; pixelY < endY; pixelY++)
        {
            for (int pixelX = startX; pixelX < endX; pixelX++)
            {
                if (texture.GetPixel(pixelX, pixelY).a <= alphaCutoff)
                {
                    continue;
                }

                foundOpaquePixel = true;
                minPixelX = Mathf.Min(minPixelX, pixelX);
                maxPixelX = Mathf.Max(maxPixelX, pixelX);
                minPixelY = Mathf.Min(minPixelY, pixelY);
                maxPixelY = Mathf.Max(maxPixelY, pixelY);
            }
        }

        if (!foundOpaquePixel)
        {
            return false;
        }

        float pixelsPerUnit = sprite.pixelsPerUnit;
        Vector2 pivot = sprite.pivot;
        bounds = new Bounds2D
        {
            MinX = (minPixelX - pivot.x) / pixelsPerUnit,
            MaxX = (maxPixelX + 1 - pivot.x) / pixelsPerUnit,
            MinY = (minPixelY - pivot.y) / pixelsPerUnit,
            MaxY = (maxPixelY + 1 - pivot.y) / pixelsPerUnit
        };
        return true;
    }

    private static bool TryGetPhysicsShapeBounds(Sprite sprite, out Bounds2D bounds)
    {
        bounds = default;

        int shapeCount = sprite.GetPhysicsShapeCount();
        if (shapeCount <= 0)
        {
            return false;
        }

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        List<Vector2> path = new List<Vector2>(64);

        for (int shapeIndex = 0; shapeIndex < shapeCount; shapeIndex++)
        {
            path.Clear();
            sprite.GetPhysicsShape(shapeIndex, path);

            for (int pointIndex = 0; pointIndex < path.Count; pointIndex++)
            {
                Vector2 point = path[pointIndex];
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxY = Mathf.Max(maxY, point.y);
            }
        }

        if (minX > maxX || minY > maxY)
        {
            return false;
        }

        bounds = new Bounds2D
        {
            MinX = minX,
            MaxX = maxX,
            MinY = minY,
            MaxY = maxY
        };
        return true;
    }
}
