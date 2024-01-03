using UnityEngine;

public static class SphereBetweenLinesHelper
{
    public static bool IsSphereBetweenLines(Vector3 lineStart, Vector3 lineEnd, Vector3 sphereCenter, float sphereRadius)
    {
        // Calculate the closest point on the line to the sphere's center
        Vector3 closestPointOnLine = GetClosestPointOnLine(lineStart, lineEnd, sphereCenter);

        // Calculate the distance between the sphere's center and the closest point on the line
        float distanceToLine = Vector3.Distance(sphereCenter, closestPointOnLine);

        // Check if the sphere is between the lines
        return distanceToLine < sphereRadius;
    }

    // Get the closest point on a line to a given point
    private static Vector3 GetClosestPointOnLine(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 lineDirection = (lineEnd - lineStart).normalized;
        float closestPointDistance = Vector3.Dot((point - lineStart), lineDirection);
        return lineStart + (closestPointDistance * lineDirection);
    }

    public static bool IsPointWithinDistance(Vector3 position, Vector3 point, float distance)
    {
        float distanceToPosition = Vector3.Distance(position, point);
        return distanceToPosition <= distance;
    }
}
