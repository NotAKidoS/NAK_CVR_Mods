using UnityEngine;

namespace Popcron;

internal class Element
{
    public Vector3[] points = { };
    public Color color = Color.white;
    public bool dashed = false;
    public Matrix4x4 matrix = Matrix4x4.identity;
}