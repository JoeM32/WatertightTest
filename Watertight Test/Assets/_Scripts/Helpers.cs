using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Linq;
using System.Drawing;
using Color = UnityEngine.Color;
using TMPro;
using Unity.VisualScripting;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Text;

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif
public static class Helpers
{
    private static MaterialPropertyBlock _propertyBlock;
    public static MaterialPropertyBlock PropertyBlock
    {
        get
        {
            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();
            return _propertyBlock;
        }
    }


    public class UnorderedPairComparer<T> : IEqualityComparer<(T, T)>
    {
        public bool Equals((T, T) x, (T, T) y)
        {
            return x.Item1.Equals(y.Item1) && x.Item2.Equals(y.Item2) ||
                   x.Item1.Equals(y.Item2) && x.Item2.Equals(y.Item1);
        }

        public int GetHashCode((T, T) obj)
        {
            return obj.Item1.GetHashCode() ^ obj.Item2.GetHashCode();
        }
    }

    public static string CamelCaseToSpaces(string camel)
    {
        camel = Regex.Replace(camel, "(\\B[A-Z])", " $1");
        camel = camel.TrimStart(' ');
        return camel;
    }

    public static Vector3 GetFlatIntersectionKnown(Vector3 l1_p1, Vector3 l1_p2, Vector3 l2_p1, Vector3 l2_p2)
    {
        float denominator = (l2_p2.z - l2_p1.z) * (l1_p2.x - l1_p1.x) - (l2_p2.x - l2_p1.x) * (l1_p2.z - l1_p1.z);

        float u_a = ((l2_p2.x - l2_p1.x) * (l1_p1.z - l2_p1.z) - (l2_p2.z - l2_p1.z) * (l1_p1.x - l2_p1.x)) / denominator;

        Vector3 intersectionPoint = l1_p1 + u_a * (l1_p2 - l1_p1);

        return intersectionPoint;
    }
    public static float QuadraticEvaluate(float t, float steepness = 2.0f)
    {
        t = Mathf.Clamp01(t);
        if (t < 0.5f)
        {
            t *= 2.0f;

            t = Mathf.Pow(t, steepness);

            t /= 2.0f;
        }
        else if (t > 0.5f)
        {
            t -= 0.5f;
            t *= 2.0f;

            t = 1 - Mathf.Pow(1 - t, steepness);

            t /= 2.0f;
            t += 0.5f;
        }
        return t;
    }

    public static float GetFlatAngleBetweenLines(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        //positvie if p1 = p3 and p2 is right of p4 relative to p1
        Vector3 line1 = p1 - p2;
        Vector3 line2 = p3 - p4;
        return Vector2.SignedAngle(new Vector2(line1.x, line1.z).normalized, new Vector2(line2.x, line2.z).normalized);
    }

    public static bool IsPointBetweenLines(Vector3 lineStart, Vector3 lineEnd1, Vector3 lineEnd2, Vector3 point)
    {
        float angle = Helpers.GetFlatAngleBetweenLines(lineStart, lineEnd1, lineStart, lineEnd2);
        float pointAngle = Helpers.GetFlatAngleBetweenLines(lineStart, lineEnd1, lineStart, point);
        if (Helpers.GetSign(angle) == Helpers.GetSign(pointAngle) && Mathf.Abs(pointAngle) < Mathf.Abs(angle))
        {
            return true;
        }
        return false;
    }

    public static Vector3 NearestPointOnLine(Vector3 linePnt, Vector3 lineDir, Vector3 pnt)
    {
        lineDir.Normalize();//this needs to be a unit vector
        var v = pnt - linePnt;
        var d = Vector3.Dot(v, lineDir);
        return linePnt + lineDir * d;
    }

    public static bool GetFlatIntersectionUnknown(Vector3 aStart, Vector3 aEnd, Vector3 bStart, Vector3 bEnd, out Vector3 intersection)
    {
        bool isIntersecting = false;
        intersection = Vector3.zero;
        //3d -> 2d
        Vector2 l1_start = new Vector2(aStart.x, aStart.z);
        Vector2 l1_end = new Vector2(aEnd.x, aEnd.z);

        Vector2 l2_start = new Vector2(bStart.x, bStart.z);
        Vector2 l2_end = new Vector2(bEnd.x, bEnd.z);

        //Direction of the lines
        Vector2 l1_dir = (l1_end - l1_start).normalized;
        Vector2 l2_dir = (l2_end - l2_start).normalized;

        //If we know the direction we can get the normal vector to each line
        Vector2 l1_normal = new Vector2(-l1_dir.y, l1_dir.x);
        Vector2 l2_normal = new Vector2(-l2_dir.y, l2_dir.x);


        //Step 1: Rewrite the lines to a general form: Ax + By = k1 and Cx + Dy = k2
        //The normal vector is the A, B
        float A = l1_normal.x;
        float B = l1_normal.y;

        float C = l2_normal.x;
        float D = l2_normal.y;

        //To get k we just use one point on the line
        float k1 = (A * l1_start.x) + (B * l1_start.y);
        float k2 = (C * l2_start.x) + (D * l2_start.y);


        //Step 2: are the lines parallel? -> no solutions
        if (IsParallel(l1_normal, l2_normal))
        {
            //Debug.Log("The lines are parallel so no solutions!");

            return isIntersecting;
        }


        //Step 3: are the lines the same line? -> infinite amount of solutions
        //Pick one point on each line and test if the vector between the points is orthogonal to one of the normals
        if (IsOrthogonal(l1_start - l2_start, l1_normal))
        {
           // Debug.Log("Same line so infinite amount of solutions!");

            //Return false anyway
            return isIntersecting;
        }


        //Step 4: calculate the intersection point -> one solution
        float x_intersect = (D * k1 - B * k2) / (A * D - B * C);
        float y_intersect = (-C * k1 + A * k2) / (A * D - B * C);

        Vector2 intersectPoint = new Vector2(x_intersect, y_intersect);


        //Step 5: but we have line segments so we have to check if the intersection point is within the segment
        if (IsBetween(l1_start, l1_end, intersectPoint) && IsBetween(l2_start, l2_end, intersectPoint))
        {
            //Debug.Log("We have an intersection point!");
            float yAverage = (aStart.y + aEnd.y + bStart.y + bEnd.y) / 4.0f;
            intersection = new Vector3(intersectPoint.x, yAverage, intersectPoint.y);
            isIntersecting = true;
        }

        return isIntersecting;
    }

    public static float IsAPointLeftOfVectorOrOnTheLine(Vector3 a, Vector3 b, Vector3 p)
    {
        float determinant = (a.x - p.x) * (b.z - p.z) - (a.z - p.z) * (b.x - p.x);

        return determinant;
    }

    //Are 2 vectors parallel?
    public static bool IsParallel(Vector2 v1, Vector2 v2)
    {
        //2 vectors are parallel if the angle between the vectors are 0 or 180 degrees
        if (Vector2.Angle(v1, v2) == 0f || Vector2.Angle(v1, v2) == 180f)
        {
            return true;
        }

        return false;
    }

    //Are 2 vectors orthogonal?
    public static bool IsOrthogonal(Vector2 v1, Vector2 v2)
    {
        //2 vectors are orthogonal is the dot product is 0
        //We have to check if close to 0 because of floating numbers
        if (Mathf.Abs(Vector2.Dot(v1, v2)) < 0.000001f)
        {
            return true;
        }

        return false;
    }

    //Is a point c between 2 other points a and b?
    public static bool IsBetween(Vector2 a, Vector2 b, Vector2 c)
    {
        bool isBetween = false;

        //Entire line segment
        Vector2 ab = b - a;
        //The intersection and the first point
        Vector2 ac = c - a;

        //Need to check 2 things: 
        //1. If the vectors are pointing in the same direction = if the dot product is positive
        //2. If the length of the vector between the intersection and the first point is smaller than the entire line
        if (Vector2.Dot(ab, ac) > 0f && ab.sqrMagnitude >= ac.sqrMagnitude)
        {
            isBetween = true;
        }

        return isBetween;
    }

    public static float Distance(float a, float b)
    {
        return Mathf.Abs(a - b);
    }

    public static int Distance(int a, int b)
    {
        return Mathf.Abs(a - b);
    }

    public static bool GetZPointOnLineFromGivenX(Vector3 lineStart, Vector3 lineEnd, float x, out float z)
    {
        if (x > Mathf.Min(lineStart.x, lineEnd.x) && x < Mathf.Max(lineStart.x, lineEnd.x))
        {
            Vector3 lineRelative = lineEnd - lineStart;
            lineRelative.y = 0;
            float gradient = lineRelative.z / lineRelative.x;

            float c = lineStart.z - (gradient * lineStart.x);
            z = (gradient * x) + c;
            return true;
        }
        else
        {
            z = 0;
            return false;
        }
    }

    public static bool GetXPointOnLineFromGivenZ(Vector3 lineStart, Vector3 lineEnd, float z, out float x)
    {
        if (z > Mathf.Min(lineStart.z, lineEnd.z) && z < Mathf.Max(lineStart.z, lineEnd.z))
        {
            Vector3 lineRelative = lineEnd - lineStart;
            lineRelative.y = 0;
            float gradient = lineRelative.z / lineRelative.x;

            float c = lineStart.z - (gradient * lineStart.x);
            x = (z - c) / gradient;
            return true;
        }
        else
        {
            x = 0;
            return false;
        }
    }

    public static string CleanString(string str)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while(index < str.Length && !char.IsLetter(str[index]))
        {
            index++;
        }
        if(index < str.Length )
        {
            builder.Append(str.ToUpper()[index]);
            index++;
            bool capitalizeNext = false;
            while (index < str.Length)
            {
                char character = str[index];
                if (char.IsLetter(character)) 
                {
                    if (capitalizeNext)
                    {
                        builder.Append(str.ToUpper()[index]);
                        capitalizeNext= false;
                    }
                    else
                    {
                        builder.Append(character);
                    }
                }
                else if(char.IsDigit(character))
                {
                    builder.Append(character);
                    capitalizeNext = true;
                }
                else
                {
                    capitalizeNext = true;
                }
                index++;
            }
        }
        return builder.ToString();
    }


    public static float CosAngle(float a, float b, float c)
    {
        if (!float.IsNaN(Mathf.Acos((-(c * c) + (a * a) + (b * b)) / (-2 * a * b)) * Mathf.Rad2Deg))
        {
            return Mathf.Acos((-(c * c) + (a * a) + (b * b)) / (2 * a * b)) * Mathf.Rad2Deg;
        }
        else
        {
            return 1;
        }
    }

    private static readonly Vector4[] s_UnitSphere = MakeUnitSphere(16);

    private static Vector4[] MakeUnitSphere(int len)
    {
        Debug.Assert(len > 2);
        var v = new Vector4[len * 3];
        for (int i = 0; i < len; i++)
        {
            var f = i / (float)len;
            float c = Mathf.Cos(f * (float)(Math.PI * 2.0));
            float s = Mathf.Sin(f * (float)(Math.PI * 2.0));
            v[0 * len + i] = new Vector4(c, s, 0, 1);
            v[1 * len + i] = new Vector4(0, c, s, 1);
            v[2 * len + i] = new Vector4(s, 0, c, 1);
        }
        return v;
    }



    public static void DebugDrawSphere(Vector4 pos, float radius, Color color, float duration = 0)
    {
        Vector4[] v = s_UnitSphere;
        int len = s_UnitSphere.Length / 3;
        for (int i = 0; i < len; i++)
        {
            var sX = pos + radius * v[0 * len + i];
            var eX = pos + radius * v[0 * len + (i + 1) % len];
            var sY = pos + radius * v[1 * len + i];
            var eY = pos + radius * v[1 * len + (i + 1) % len];
            var sZ = pos + radius * v[2 * len + i];
            var eZ = pos + radius * v[2 * len + (i + 1) % len];
            if (duration != 0)
            {
                Debug.DrawLine(sX, eX, color, duration);
                Debug.DrawLine(sY, eY, color, duration);
                Debug.DrawLine(sZ, eZ, color, duration);
            }
            else
            {
                Debug.DrawLine(sX, eX, color);
                Debug.DrawLine(sY, eY, color);
                Debug.DrawLine(sZ, eZ, color);
            }
        }
    }

    public static Vector3 ExtractTranslationFromMatrix(Matrix4x4 matrix)
    {
        Vector3 translate;
        translate.x = matrix.m03;
        translate.y = matrix.m13;
        translate.z = matrix.m23;
        return translate;
    }

    public static Quaternion ExtractRotationFromMatrix(Matrix4x4 matrix)
    {
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;

        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;

        return Quaternion.LookRotation(forward, upwards);
    }

    public static Vector3 ExtractScaleFromMatrix(ref Matrix4x4 matrix)
    {
        Vector3 scale;
        scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
        scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
        scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
        return scale;
    }

    public static Quaternion AverageQuaternions(List<Quaternion> rotations, List<float> weights)
    {
        int count = rotations.Count;
        Vector3 forwardSum = Vector3.zero;
        Vector3 upwardSum = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            forwardSum += weights[i] * (rotations[i] * Vector3.forward);
            upwardSum += weights[i] * (rotations[i] * Vector3.up);
        }
        forwardSum /= (float)count;
        upwardSum /= (float)count;

        return Quaternion.LookRotation(forwardSum, upwardSum);
    }

    public static Vector2 Project(Vector2 vectorToProject, Vector2 vectorToProjectOnto)
    {
        float dotProduct = Vector2.Dot(vectorToProject, vectorToProjectOnto);

        // Calculate the length squared of the vector you're projecting onto.
        float lengthSquared = vectorToProjectOnto.sqrMagnitude;

        // Calculate the projection of vectorToProject onto vectorToProjectOnto.
        Vector2 projection = (dotProduct / lengthSquared) * vectorToProjectOnto;

        return projection;
        // You can now use the 'projection' Vector2 for your desired purpose.

    }


    public static uint GetRendererLayers(List<int> components)
    {
        uint filteredRenderingLayer = uint.MaxValue;
        if (components != null && components.Count > 0)
        {
            filteredRenderingLayer = 0;
            foreach (uint a in components)
            {
                int b = ((int)1 << (int)a);
                filteredRenderingLayer |= (uint)b;
            }
        }
        return filteredRenderingLayer;
    }

    public static float GetSign(float value)
    {
        if(value > 0)
        {
            return 1.0f;
        }
        else if(value < 0)
        {
            return -1.0f;
        }
        else
        {
            return 0;
        }
    }

 

    public static Bounds CalculateOverlapBounds(Bounds bounds1, Bounds bounds2)
    {
        Vector3 min = Vector3.Max(bounds1.min, bounds2.min);
        Vector3 max = Vector3.Min(bounds1.max, bounds2.max);

        Bounds overlapBounds = new Bounds();
        overlapBounds.SetMinMax(min, max);

        return overlapBounds;
    }

    public static bool BetweenValues(float min, float max, float value)
    {
        return value >= min && value <= max;
    }

    public static bool BetweenValues(int min, int max, int value)
    {
        return value >= min && value <= max;
    }
    public static Vector2 NormalizeScreenPos(Vector2 screenPos)
    {
        float largest = Screen.width < Screen.height ? Screen.width : Screen.height;
        screenPos.x /= largest;
        screenPos.y /= largest;
        return screenPos;
    }
    public static Vector2 FlattenVector2(this Vector3 vector3)
    {
        return new Vector2(vector3.x, vector3.z);
    }

    public static Vector3 UnFlattenVector3(this Vector2 vector2)
    {
        return new Vector3(vector2.x, 0, vector2.y);
    }

    public static float FlatDistance(Vector3 a, Vector3 b)
    {
        return Mathf.Sqrt(Mathf.Pow(a.x - b.x, 2.0f)+Mathf.Pow(a.y - b.y, 2.0f));
    }

    public static T WrappedAccess<T>(this T[] array, int index)
    {
        if(index >=array.Length)
        {
            index -= ((index / array.Length) * array.Length);
        }
        else if(index < 0)
        {
            index += (((Mathf.Abs(index) / array.Length) + 1) * array.Length);
        }
        return array[index];
    }

    public static bool Approximately(float a, float b, float threshold = 0)
    {
        if (Mathf.Abs(threshold) > 0f)
        {
            return Mathf.Abs(a - b) <= Mathf.Abs(threshold);
        }
        else
        {
            return Mathf.Approximately(a, b);
        }
    }



    public static bool Approximately(Vector2 a, Vector2 b, float threshold = 0)
    {
        if (Mathf.Abs(threshold) > 0f)
        {
            return Mathf.Abs(a.x - b.x) <= Mathf.Abs(threshold) && Mathf.Abs(a.y - b.y) <= Mathf.Abs(threshold);
        }
        else
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
        }
    }

    public static bool Approximately(Vector3 a, Vector3 b, float threshold = 0)
    {
        if (Mathf.Abs(threshold) > 0f)
        {
            return Mathf.Abs(a.x - b.x) <= Mathf.Abs(threshold) && Mathf.Abs(a.y - b.y) <= Mathf.Abs(threshold) && Mathf.Abs(a.z - b.z) <= Mathf.Abs(threshold);
        }
        else
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z);
        }
    }

    public static bool Approximately(Color colorA, Color colorB, float threshold, bool includeAlpha = false)
    {
        if(includeAlpha)
        {
            Debug.LogError("Not implemented");
            return false;
        }
        if (threshold > 0f)
        {
            return Mathf.Abs(colorA.r - colorB.r) <= threshold && Mathf.Abs(colorA.g - colorB.g) <= threshold && Mathf.Abs(colorA.b - colorB.b) <= threshold;
        }
        else
        {
            return Mathf.Approximately(colorA.r, colorB.r) && Mathf.Approximately(colorA.g, colorB.g) && Mathf.Approximately(colorA.b, colorB.b);
        }
    }

    public static float ColorDifference(Color colorA, Color colorB, bool includeAlpha = false)
    {
        if (includeAlpha)
        {
            Debug.LogError("Not implemented");
            return 0;
        }

        //Color.RGBToHSV(colorA, out float hueA, out float saturationA, out float valueA);
        //Vector3 hsvA = new Vector3(hueA, saturationA, valueA);
        //Color.RGBToHSV(colorB, out float hueB, out float saturationB, out float valueB);
        //Vector3 hsvB = new Vector3(hueB, saturationB, valueB);
        //return Vector3.Distance(hsvA, hsvB);

        return ((Vector4)(colorA - colorB)).magnitude;
    }


    public static float ManhattanDistance(Vector3 positionA, Vector3 positionB)
    {
        Vector3 distanceVector = positionA - positionB;
        return Mathf.Abs(distanceVector.x) + Mathf.Abs(distanceVector.y) + Mathf.Abs(distanceVector.z);
    }

    public static float CalculateTriangleArea(Vector3 pointA, Vector3 pointB, Vector3 pointC)
    {
        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        float area = 0.5f * Vector3.Cross(sideAB, sideAC).magnitude;

        return area;
    }

    public static float GetVerticalAngleRelativeTo(Vector3 positionA, Vector3 positionB)
    {
        Vector3 direction = positionA - positionB;
        float verticalAngle = Vector3.Angle(direction, Vector3.up);
        return Mathf.Abs(verticalAngle - 90);
    }

    public static T WrappedAccess<T>(this List<T> list, int index)
    {
        //Debug.Log("before" + list.Count + "-" + index);

        if (index >= list.Count)
        {
            index -= ((index / list.Count) * list.Count);
        }
        else if (index < 0)
        {
            int listAmount = (Mathf.Abs(index + 1) / list.Count) + 1;
            index = (list.Count * listAmount) + index;
        }
        if(list.Count == 1)
        {
            index = 0;
        }
        else if(list.Count == 0)
        {
            Debug.LogError("Cannot perform wrapped access on an empty list");
        }
        //Debug.Log("after" + list.Count + "-" + index);
        return list[index];
    }

    public static float FlatDotProduct(Vector3 a, Vector3 b)
    {
        a.y = 0;
        b.y = 0;
        return Vector3.Dot(a.normalized, b.normalized);
    }

    public static float GetNormalizedIndexValue(int index, int arraySize)
    {
        if(index == 0)
        {
            return 0;
        }
        else if(index == arraySize - 1)
        {
            return 1;
        }
        else
        {
            float indexFloat = index;
            float arraySizeFloat = arraySize;
            return (indexFloat) / (arraySizeFloat - 1);
        }
    }

    public static bool OBBContainsPoint(Vector3 OBBposition, Quaternion OBBrotation, Vector3 OBBscale, Vector3 point)
    {
        Matrix4x4 m = Matrix4x4.TRS(OBBposition, OBBrotation, Vector3.one);
        Vector3 halfSize = OBBscale / 2.0f;
        point = m.inverse.MultiplyPoint3x4(point);
        //p = rotation * p - center;
        return point.x <= halfSize.x && point.x > -halfSize.x
        && point.y <= halfSize.y && point.y > -halfSize.y
        && point.z <= halfSize.z && point.z > -halfSize.z;
    }

    public static bool OverlapOBBOBB(Vector3 positionA, Quaternion rotationA, Vector3 scaleA, Vector3 positionB, Quaternion rotationB, Vector3 scaleB)
    {
        Vector3 minOverlapAxis = Vector3.zero;

        float FindScalarProjection(Vector3 point, Vector3 axis)
        {
            return Vector3.Dot(point, axis);
        }

        float FindOverlap(float astart, float aend, float bstart, float bend)
        {
            if (astart < bstart)
            {
                if (aend < bstart)
                {
                    return 0f;
                }

                return aend - bstart;
            }

            if (bend < astart)
            {
                return 0f;
            }

            return bend - astart;
        }

        bool ProjectionHasOverlap(int aAxesLength,Vector3[] aAxes, int bVertsLength,Vector3[] bVertices, int aVertsLength,Vector3[] aVertices)
        {
            float minOverlap = float.PositiveInfinity;

            for (int i = 0; i < aAxesLength; i++)
            {


                float bProjMin = float.MaxValue, aProjMin = float.MaxValue;
                float bProjMax = float.MinValue, aProjMax = float.MinValue;

                Vector3 axis = aAxes[i];

                // Handles the cross product = {0,0,0} case
                if (aAxes[i] == Vector3.zero) return true;

                for (int j = 0; j < bVertsLength; j++)
                {
                    float val = FindScalarProjection((bVertices[j]), axis);

                    if (val < bProjMin)
                    {
                        bProjMin = val;
                    }

                    if (val > bProjMax)
                    {
                        bProjMax = val;
                    }
                }

                for (int j = 0; j < aVertsLength; j++)
                {
                    float val = FindScalarProjection((aVertices[j]), axis);

                    if (val < aProjMin)
                    {
                        aProjMin = val;
                    }

                    if (val > aProjMax)
                    {
                        aProjMax = val;
                    }
                }

                float overlap = FindOverlap(aProjMin, aProjMax, bProjMin, bProjMax);

                if (overlap < minOverlap)
                {
                    minOverlap = overlap;
                    minOverlapAxis = axis;

                }

                //Debug.Log(overlap);

                if (overlap <= 0)
                {
                    // Separating Axis Found Early Out
                    return false;
                }
            }

            return true; // A penetration has been found
        }

        Vector3[] aAxes = new[]
        {
            (rotationA * Vector3.right),
            (rotationA * Vector3.up),
            (rotationA * Vector3.forward)
        };
        Vector3[] bAxes = new[]
        {
            (rotationB * Vector3.right),
            (rotationB * Vector3.up),
            (rotationB * Vector3.forward)
        };

        Vector3[] AllAxes = new Vector3[]
        {
            aAxes[0],
            aAxes[1],
            aAxes[2],
            bAxes[0],
            bAxes[1],
            bAxes[2],
            Vector3.Cross(aAxes[0], bAxes[0]),
            Vector3.Cross(aAxes[0], bAxes[1]),
            Vector3.Cross(aAxes[0], bAxes[2]),
            Vector3.Cross(aAxes[1], bAxes[0]),
            Vector3.Cross(aAxes[1], bAxes[1]),
            Vector3.Cross(aAxes[1], bAxes[2]),
            Vector3.Cross(aAxes[2], bAxes[0]),
            Vector3.Cross(aAxes[2], bAxes[1]),
            Vector3.Cross(aAxes[2], bAxes[2])
        };

        int aAxesLength = aAxes.Length;
        int bAxesLength = bAxes.Length;

        Vector3 halfSizeA = scaleA / 2.0f;
        Vector3 halfSizeB = scaleB / 2.0f;

        Vector3[] aVertices = new Vector3[]{
            positionA + rotationA * new Vector3(-halfSizeA.x,halfSizeA.y,halfSizeA.z),
            positionA + rotationA * new Vector3(halfSizeA.x,-halfSizeA.y,halfSizeA.z),
            positionA + rotationA * new Vector3(halfSizeA.x,halfSizeA.y,-halfSizeA.z),
            positionA + rotationA * new Vector3(halfSizeA.x,halfSizeA.y,halfSizeA.z),
            positionA + rotationA * new Vector3(-halfSizeA.x,-halfSizeA.y,-halfSizeA.z),
            positionA + rotationA * new Vector3(-halfSizeA.x,-halfSizeA.y,halfSizeA.z),
            positionA + rotationA * new Vector3(halfSizeA.x,-halfSizeA.y,-halfSizeA.z),
            positionA + rotationA * new Vector3(-halfSizeA.x,halfSizeA.y,-halfSizeA.z),
        };

        Vector3[] bVertices = new Vector3[]{
            positionB + rotationB * new Vector3(-halfSizeB.x,halfSizeB.y,halfSizeB.z),
            positionB + rotationB * new Vector3(halfSizeB.x,-halfSizeB.y,halfSizeB.z),
            positionB + rotationB * new Vector3(halfSizeB.x,halfSizeB.y,-halfSizeB.z),
            positionB + rotationB * new Vector3(halfSizeB.x,halfSizeB.y,halfSizeB.z),
            positionB + rotationB * new Vector3(-halfSizeB.x,-halfSizeB.y,-halfSizeB.z),
            positionB + rotationB * new Vector3(-halfSizeB.x,-halfSizeB.y,halfSizeB.z),
            positionB + rotationB * new Vector3(halfSizeB.x,-halfSizeB.y,-halfSizeB.z),
            positionB + rotationB * new Vector3(-halfSizeB.x,halfSizeB.y,-halfSizeB.z),
        };

        int aVertsLength = aVertices.Length;
        int bVertsLength = bVertices.Length;

        bool hasOverlap = false;

        if (ProjectionHasOverlap(AllAxes.Length, AllAxes, bVertsLength, bVertices, aVertsLength, aVertices))
        {
            hasOverlap = true;
        }
        else if (ProjectionHasOverlap( AllAxes.Length, AllAxes, aVertsLength, aVertices, bVertsLength, bVertices))
        {
            hasOverlap = true;
        }

        // Penetration can be seen here, but its not reliable 

        return hasOverlap;
    }


    public static Dictionary<V, K> FlipDictionary<K, V>(Dictionary<K, V> dictionary)
    {
        Dictionary<V, K> flippedDictionary = new Dictionary<V, K>();
        foreach (var pair in dictionary)
        {
            flippedDictionary.Add(pair.Value, pair.Key);
        }
        return flippedDictionary;
    }

    /// <summary>
    /// Hacky fix because loading an asset during import returns null, so we get the name from the filepath.
    /// </summary>
    /// <returns>The scene name</returns>
    public static string GetSceneAssetName(string filepath)
    {
        string[] split = filepath.Split("/");
        string name = split[split.Length - 1];
        return name.Substring(0, name.Length - 6);
    }

    public static void Resize<T>(this List<T> list, int newSize, T fillValue)
    {
        int cur = list.Count;
        if (newSize < cur)
            list.RemoveRange(newSize, cur - newSize);
        else if (newSize > cur)
        {
            if (newSize > list.Capacity)//this bit is purely an optimisation, to avoid multiple automatic capacity changes.
                list.Capacity = newSize;
            list.AddRange(Enumerable.Repeat(fillValue, newSize - cur));
        }
    }
    public static void Resize<T>(this List<T> list, int sz) where T : new()
    {
        Resize(list, sz, new T());
    }

    public static void PurgeDirectory(string target_dir)
    {
        string[] files = Directory.GetFiles(target_dir);
        string[] dirs = Directory.GetDirectories(target_dir);

        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (string dir in dirs)
        {
            PurgeDirectory(dir);
        }

        Directory.Delete(target_dir, false);
    }

}


#if UNITY_EDITOR
public static class EditorHelpers
{
    public static void RunAfterReserialization(Action function)
    {
        EditorCoroutineUtility.StartCoroutine(RunAfterReserializationRoutine(function), ScriptableObject.CreateInstance<EditorWindow>());
    }

    public static string GetAssetFolder(UnityEngine.Object asset)
    {
        string path = AssetDatabase.GetAssetPath(asset);
        string[] split = path.Split('/');
        return path.Substring(0, path.Length - split[split.Length - 1].Length);

    }

    private static IEnumerator RunAfterReserializationRoutine(Action function)
    {
        yield return null;//first frame to end compilation
        yield return null;//second frame is serialized data being laoded into compiled scriptable objects.
        function.Invoke();
    }

    private static IEnumerable<System.Type> AllPropertyDrawers()
    {
        List<System.Type> drawers = new List<Type>();
        foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type t in ass.GetTypes())
            {
                if (typeof(PropertyDrawer).IsAssignableFrom(t))
                {
                    yield return t;
                }
            }
        }
    }

    public static System.Type GetCustomPropertyDrawerFor(System.Type target)
    {
        List<Type> drawers = new List<Type>();
        foreach (Type drawer in AllPropertyDrawers())
        {
            if(drawer.ReflectedType == target)
            { 
                return drawer;
            }
        }
        return null;

    }

    private static T GetFieldValue<T>(this object obj, string name)
    {
        // Set the flags so that private and public fields from instances will be found
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var field = obj.GetType().GetField(name, bindingFlags);
        return (T)field?.GetValue(obj);
    }

    //public static T Clone<T>(this T item)
    //{
    //    FieldInfo[] fis = item.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    //    object tempMyClass = Activator.CreateInstance(item.GetType());
    //    foreach (FieldInfo fi in fis)
    //    {
    //        if (fi.FieldType.Namespace != item.GetType().Namespace)
    //            fi.SetValue(tempMyClass, fi.GetValue(item));
    //        else
    //        {
    //            object obj = fi.GetValue(item);
    //            fi.SetValue(tempMyClass, obj.Clone());
    //        }
    //    }
    //    return (T)tempMyClass;
    //}

}
#endif