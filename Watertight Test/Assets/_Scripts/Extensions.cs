using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

public static class Extensions
{
    public static Vector2 Rotate(this Vector2 v, float angleDeg)
    {
        float delta = Mathf.Deg2Rad * angleDeg;
        return new Vector2(
            v.x * Mathf.Cos(delta) - v.y * Mathf.Sin(delta),
            v.x * Mathf.Sin(delta) + v.y * Mathf.Cos(delta)
        );
    }

    public static bool GetValue(this bool? b)
    {
        if (b == null)
        {
            return false;
        }
        else
        {
            return (bool)b;
        }
    }

    public static Vector3[] GetCorners(this Bounds bounds)
    {
        Vector3 boundPoint1 = bounds.min;
        Vector3 boundPoint2 = bounds.max;
        Vector3 boundPoint3 = new Vector3(boundPoint1.x, boundPoint1.y, boundPoint2.z);
        Vector3 boundPoint4 = new Vector3(boundPoint1.x, boundPoint2.y, boundPoint1.z);
        Vector3 boundPoint5 = new Vector3(boundPoint2.x, boundPoint1.y, boundPoint1.z);
        Vector3 boundPoint6 = new Vector3(boundPoint1.x, boundPoint2.y, boundPoint2.z);
        Vector3 boundPoint7 = new Vector3(boundPoint2.x, boundPoint1.y, boundPoint2.z);
        Vector3 boundPoint8 = new Vector3(boundPoint2.x, boundPoint2.y, boundPoint1.z);

        Vector3[] vertices = new[] { boundPoint1, boundPoint2, boundPoint3, boundPoint4, boundPoint5, boundPoint6, boundPoint7, boundPoint8 };
        return vertices;    
    }

    public static float GetScaledRadius(this CapsuleCollider collider) 
    {
        Vector3 scale = collider.transform.lossyScale;
        return Mathf.Abs(Mathf.Max(Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)), Mathf.Abs(scale.z)) * collider.radius);
    }

public static Mesh GetMesh(this Bounds bounds)
    {
        var mesh = new Mesh();
        Vector3[] vertices = bounds.GetCorners();
        mesh.vertices = vertices;
        mesh.triangles = new[]
        {
            0,7,4,
            0,3,7,
            5,1,3,
            3,1,7,
            7,1,4,
            4,1,6,
            5,3,2,
            2,3,0,
            0,4,2,
            2,4,6,
            1,5,2,
            6,1,2
            };
        mesh.Optimize();
        mesh.RecalculateNormals();
        return mesh;
    }
    public static bool HasFlagSet<T>(this T a, T b) where T : Enum
    {
        return a.HasFlag(b);
    }
    public static string ToCamelCase(this string str)
    {
        if (str == null || str.Length == 0 || str == " ") return "";

        var words = str.Split(new[] { "_", " " }, StringSplitOptions.RemoveEmptyEntries);
        var leadWord = Regex.Replace(words[0], @"([A-Z])([A-Z]+|[a-z0-9]+)($|[A-Z]\w*)",
            m =>
            {
                return m.Groups[1].Value.ToLower() + m.Groups[2].Value.ToLower() + m.Groups[3].Value;
            });
        var tailWords = words.Skip(1)
            .Select(word => char.ToUpper(word[0]) + word.Substring(1))
            .ToArray();
        return $"{leadWord}{string.Join(string.Empty, tailWords)}";
    }

    public static bool ContainsPoint(this Collider collider, Vector3 point)
    {
        return (collider.ClosestPoint(point) == point);
    }

    public static void SetActiveConditional(this GameObject gameObject, bool active)
    {
        if (active)
        {
            if (gameObject.activeSelf == false)
            {
                gameObject.SetActive(true);
            }
        }
        else
        {
            if (gameObject.activeSelf == true)
            {
                gameObject.SetActive(false);
            }
        }
    }

    public static float EvaluateNornalized(this AnimationCurve curve, float normalizedTime)
    {
        if (curve.keys.Length == 0)
        {
            return 0;
        }
        float end = curve.keys[curve.keys.Length - 1].time;
        return curve.Evaluate(Mathf.Lerp(0, end, normalizedTime));
    }

    public static T[] GetSetFlags<T>(this T a) where T : Enum
    {
        List<T> result = new List<T>();
        Array allValues = Enum.GetValues(typeof(T));
        foreach (var value in allValues.Cast<T>())
        {
            if (a.HasFlagSet(value))
            {
                result.Add(value);
            }
        }
        return result.ToArray();
    }

    public static void WrappedRemoval<T>(this List<T> list, int index, int amount)
    {
        if(amount <= list.Count && index + amount  > list.Count)
        {
            int wrapAround = (index + amount) - list.Count;
            int baseAmount = amount - wrapAround;
            list.RemoveRange(index, baseAmount);
            list.RemoveRange(0,wrapAround);
        }
        else
        {
            list.RemoveRange(index, amount);
        }
    }

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static void AddorOverride<A, B>(this Dictionary<A, B> dictionary, A key, B value)
    {
        if (dictionary.ContainsKey(key))
        {
            dictionary[key] = value;
        }
        else
        {
            dictionary.Add(key, value);
        }
    }


    public static Dictionary<V, K> Flip<K, V>(this Dictionary<K, V> dictionary)
    {
        return Helpers.FlipDictionary(dictionary);
    }

    public static void SetFloat(this Renderer renderer, string property, float value)
    {
        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
        {
            renderer.SetFloat(i, property, value);
        }
    }

    public static void SetFloat(this Renderer renderer, int materialIndex, string property, float value)
    {
        renderer.GetPropertyBlock(Helpers.PropertyBlock, materialIndex);
        Helpers.PropertyBlock.SetFloat(property, value);
        renderer.SetPropertyBlock(Helpers.PropertyBlock, materialIndex);
    }
    public static void SetInt(this Renderer renderer, string property, int value)
    {
        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
        {
            renderer.SetInt(i, property, value);
        }
    }

    public static void SetInt(this Renderer renderer, int materialIndex, string property, int value)
    {
        renderer.GetPropertyBlock(Helpers.PropertyBlock, materialIndex);
        Helpers.PropertyBlock.SetInt(property, value);
        renderer.SetPropertyBlock(Helpers.PropertyBlock, materialIndex);
    }
    public static void SetBool(this Renderer renderer, string property, bool value)
    {
        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
        {
            renderer.SetBool(i, property, value);
        }
    }
    public static void SetBool(this Renderer renderer, int materialIndex, string property, bool value)
    {
        renderer.GetPropertyBlock(Helpers.PropertyBlock, materialIndex);
        Helpers.PropertyBlock.SetFloat(property, value ? 1.0f : 0.0f);
        renderer.SetPropertyBlock(Helpers.PropertyBlock, materialIndex);
    }
    public static void SetColor(this Renderer renderer, string property, Color color)
    {
        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
        {
            renderer.SetColor(i, property, color);
        }
    }
    public static void SetColor(this Renderer renderer, int materialIndex, string property, Color color)
    {
        renderer.GetPropertyBlock(Helpers.PropertyBlock, materialIndex);
        Helpers.PropertyBlock.SetColor(property, color);
        renderer.SetPropertyBlock(Helpers.PropertyBlock, materialIndex);
    }
    public static void SetColor(this Renderer renderer, string property, Vector4 color)
    {
        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
        {
            renderer.SetColor(i, property, color);
        }
    }
    public static void SetColor(this Renderer renderer, int materialIndex, string property, Vector4 color)
    {
        renderer.GetPropertyBlock(Helpers.PropertyBlock, materialIndex);
        Helpers.PropertyBlock.SetColor(property, color);
        renderer.SetPropertyBlock(Helpers.PropertyBlock, materialIndex);
    }

    //public static void Enable(this ActionMaps.Maps enabledActionMaps)
    //{
    //    foreach (var ticked in enabledActionMaps.GetSetFlags())
    //    {
    //        InputSystem.Controls.FindActionMap(ActionMaps.lookup[ticked]).Enable();
    //    }
    //}

    //public static void Disable(this ActionMaps.Maps enabledActionMaps)
    //{
    //    foreach (var ticked in enabledActionMaps.GetSetFlags())
    //    {
    //        InputSystem.Controls.FindActionMap(ActionMaps.lookup[ticked]).Enable();
    //    }
    //}

    public static bool IsConstant(this AnimationCurve curve, out float constantValue)
    {
        constantValue = 0;
        if (curve != null)
        {
            if (curve.keys.Length >= 2)
            {
                float time = curve.keys[0].time;
                float value = curve.keys[0].value;
                for (int i = 1; i < curve.keys.Length; i++)
                {
                    if (!Mathf.Approximately(curve[i].value, value) || !Mathf.Approximately(curve[i].time, time))
                    {
                        return false;
                    }
                }
            }
            constantValue = curve.keys[0].value;
            return true;
        }
        return false;
    }

    public static T[] GetSubArray<T>(this T[] array, int startIndex)
    {
        T[] result = new T[array.Length - startIndex];
        Array.Copy(array, startIndex, result, 0, result.Length);
        return result;
    }

    public static Vector3 GetCenterInWorld(this CharacterController controller)
    {
        return controller.transform.position + controller.center;
    }

    public static Vector3 GetRandomPointInBounds(this Bounds bounds)
    {
        return new Vector3(UnityEngine.Random.Range(bounds.min.x, bounds.max.x), UnityEngine.Random.Range(bounds.min.y, bounds.max.y), UnityEngine.Random.Range(bounds.min.z, bounds.max.z));
    }

    public static Vector3 AsFlat(this Vector3 vector)
    {
        return new Vector3(vector.x, 0, vector.z);
    }

    public static float FlatMagnitude(this Vector3 vector)
    {
        return (new Vector2(vector.x, vector.z)).magnitude;
    }


    public static string GetMiddleString(this string @string, string start, string end)
    {
        int pFrom = @string.IndexOf(start) + start.Length;
        int pTo = @string.LastIndexOf(end);
        return @string.Substring(pFrom, pTo - pFrom);
    }

    public static CollisionFlags MoveTo(this CharacterController characterController, Vector3 position)
    {
        Vector3 localPosition = position - characterController.transform.position;
        return characterController.Move(localPosition);
    }

    public static void GetSpheres(this CharacterController characterController, out Vector3 top, out Vector3 bottom)
    {
        bottom = characterController.transform.position + characterController.center + (Vector3.up * -characterController.height * 0.5F);
        top = bottom + (Vector3.up * characterController.height);
    }

    public static float AverageValue(this AnimationCurve curve, int accuracy)
    {
        float value = 0;
        int samples = 0;
        for(int i = 0; i < curve.keys.Length - 1; i++)
        {
            value += curve.Evaluate(curve.keys[i].value);
            samples++;

            if (accuracy > 0)
            {
                float fraction = (1.0f / ((float)accuracy+1));
                for (int j = 1; j <= accuracy; j++)
                {
                    float difference = curve.keys[i + 1].time - curve.keys[i].time;
                    value += curve.Evaluate((curve.keys[i].time + (difference * fraction * (float)j)));
                    samples++;
                }
            }
        }
        value += curve.Evaluate(curve.keys[curve.keys.Length - 1].value);
        samples++;
        return value / (float)samples;
    }


    public static void DrawGizmos(this Bounds b, Color color, float delay = 0)
    {
        // bottom
        var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
        var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
        var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
        var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

        Debug.DrawLine(p1, p2, color, delay);
        Debug.DrawLine(p2, p3, color, delay);
        Debug.DrawLine(p3, p4, color, delay);
        Debug.DrawLine(p4, p1, color, delay);

        // top
        var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
        var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
        var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
        var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

        Debug.DrawLine(p5, p6, color, delay);
        Debug.DrawLine(p6, p7, color, delay);
        Debug.DrawLine(p7, p8, color, delay);
        Debug.DrawLine(p8, p5, color, delay);

        // sides
        Debug.DrawLine(p1, p5, color, delay);
        Debug.DrawLine(p2, p6, color, delay);
        Debug.DrawLine(p3, p7, color, delay);
        Debug.DrawLine(p4, p8, color, delay);
    }


    public static List<Component> FindInScene(this GameObject gameObject, Type type)
    {
        List<Component> components = new List<Component>();
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            components.AddRange(root.GetComponentsInChildren(type));
        }
        return components;
    }

    public static float GetLength(this NavMeshPath path)
    {
        float length = 0f;
        for(int i = 0; i < path.corners.Length - 1;i++)
        {
            length += Vector3.Distance(path.corners[i], path.corners[i+1]);
        }
        return length;
    }
}

#if UNITY_EDITOR

public static class EditorExtensions
{
    public static IEnumerable<SerializedProperty> GetChildren(this SerializedObject serializedObject, params string[] excluded)
    {
        SerializedProperty serializedProperty = serializedObject.GetIterator();
        serializedProperty.NextVisible(enterChildren: true);
        while (serializedProperty.NextVisible(enterChildren: false))
        {
            if (!excluded.Contains(serializedProperty.name))
            {
                yield return serializedProperty;
            }
        }

    }

    public static IEnumerable<SerializedProperty> GetChildren(this SerializedProperty serializedProperty)
    {
        SerializedProperty currentProperty = serializedProperty.Copy();
        SerializedProperty nextSiblingProperty = serializedProperty.Copy();
        {
            nextSiblingProperty.Next(false);
        }

        if (currentProperty.Next(true))
        {
            do
            {
                if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
                    break;

                yield return currentProperty;
            }
            while (currentProperty.Next(false));
        }
    }

    public static void DrawHandles(this Bounds b, Color color)
    {
        Handles.color = color;
        // bottom
        var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
        var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
        var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
        var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

        Handles.DrawLine(p1, p2);
        Handles.DrawLine(p2, p3);
        Handles.DrawLine(p3, p4);
        Handles.DrawLine(p4, p1);

        // top
        var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
        var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
        var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
        var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

        Handles.DrawLine(p5, p6);
        Handles.DrawLine(p6, p7);
        Handles.DrawLine(p7, p8);
        Handles.DrawLine(p8, p5);

        // sides
        Handles.DrawLine(p1, p5);
        Handles.DrawLine(p2, p6);
        Handles.DrawLine(p3, p7);
        Handles.DrawLine(p4, p8);
    }
    /// <summary>
    /// Get the object the serialized property holds by using reflection
    /// </summary>
    /// <typeparam name="T">The object type that the property contains</typeparam>
    /// <param name="property"></param>
    /// <returns>Returns the object type T if it is the type the property actually contains</returns>

    public static T GetValue<T>(this SerializedProperty property, bool applyModified = true)
    {
        if (applyModified)
        {
            property.serializedObject.ApplyModifiedProperties();
        }
        return GetNestedObject<T>(property.propertyPath, GetSerializedPropertyRootComponent(property));
    }

    /// <summary>
    /// Set the value of a field of the property with the type T
    /// </summary>
    /// <typeparam name="T">The type of the field that is set</typeparam>
    /// <param name="property">The serialized property that should be set</param>
    /// <param name="value">The new value for the specified property</param>
    /// <returns>Returns if the operation was successful or failed</returns>
    public static bool SetValue<T>(this SerializedProperty property, T value)
    {

        object obj = GetSerializedPropertyRootComponent(property);
        //Iterate to parent object of the value, necessary if it is a nested object
        string[] fieldStructure = property.propertyPath.Split('.');
        for (int i = 0; i < fieldStructure.Length - 1; i++)
        {
            if (fieldStructure[i] == "Array")
            {
                i++;
                IEnumerable iterable = obj as IEnumerable;
                int index = 0;
                int targetIndex = int.Parse(fieldStructure[i].GetMiddleString("[", "]"));
                foreach (var x in iterable)
                {
                    if (index == targetIndex)
                    {
                        obj = x;
                        break;
                    }
                    index++;
                }
            }
            else
            {
                obj = GetFieldOrPropertyValue<object>(fieldStructure[i], obj);
            }
        }
        string fieldName = fieldStructure.Last();

        return SetFieldOrPropertyValue(fieldName, obj, value);

    }

    /// <summary>
    /// Get the component of a serialized property
    /// </summary>
    /// <param name="property">The property that is part of the component</param>
    /// <returns>The root component of the property</returns>
    public static object GetSerializedPropertyRootComponent(SerializedProperty property)
    {
        return property.serializedObject.targetObject;
    }

    /// <summary>
    /// Iterates through objects to handle objects that are nested in the root object
    /// </summary>
    /// <typeparam name="T">The type of the nested object</typeparam>
    /// <param name="path">Path to the object through other properties e.g. PlayerInformation.Health</param>
    /// <param name="obj">The root object from which this path leads to the property</param>
    /// <param name="includeAllBases">Include base classes and interfaces as well</param>
    /// <returns>Returns the nested object casted to the type T</returns>
    public static T GetNestedObject<T>(string path, object obj, bool includeAllBases = false)
    {
        string[] fieldStructure = path.Split('.');
        for (int i = 0; i < fieldStructure.Length; i++)
        {
            if (fieldStructure[i] == "Array")
            {
                i++;
                IEnumerable iterable = obj as IEnumerable;
                int index = 0;
                int targetIndex = int.Parse(fieldStructure[i].GetMiddleString("[", "]"));
                foreach (var x in iterable)
                {
                    if (index == targetIndex)
                    {
                        obj = x;
                        break;
                    }
                    index++;
                }
            }
            else
            {
                obj = GetFieldOrPropertyValue<object>(fieldStructure[i], obj);
            }
        }

        //foreach (string part in path.Split('.'))
        //{
        //    obj = GetFieldOrPropertyValue<object>(part, obj, includeAllBases);
        //}

        if(obj != null)
        {
            Type type = typeof(T);
            T casted =  (T)obj;
            if(casted == null)
            {
                return default(T);
            }
            return casted;
        }
        else
        {
            return default(T);
        }
    }

    public static T GetFieldOrPropertyValue<T>(string fieldName, object obj, bool includeAllBases = false, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
    {
        FieldInfo field = obj.GetType().GetField(fieldName, bindings);
        if (field != null) return (T)field.GetValue(obj);

        PropertyInfo property = obj.GetType().GetProperty(fieldName, bindings);
        if (property != null) return (T)property.GetValue(obj, null);

        if (includeAllBases)
        {

            foreach (Type type in GetBaseClassesAndInterfaces(obj.GetType()))
            {
                field = type.GetField(fieldName, bindings);
                if (field != null) return (T)field.GetValue(obj);

                property = type.GetProperty(fieldName, bindings);
                if (property != null) return (T)property.GetValue(obj, null);
            }
        }

        return default(T);
    }

    public static bool SetFieldOrPropertyValue(string fieldName, object obj, object value, bool includeAllBases = false, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
    {
        FieldInfo field = obj.GetType().GetField(fieldName, bindings);
        if (field != null)
        {
            field.SetValue(obj, value);
            return true;
        }

        PropertyInfo property = obj.GetType().GetProperty(fieldName, bindings);
        if (property != null)
        {
            property.SetValue(obj, value, null);
            return true;
        }

        if (includeAllBases)
        {
            foreach (Type type in GetBaseClassesAndInterfaces(obj.GetType()))
            {
                field = type.GetField(fieldName, bindings);
                if (field != null)
                {
                    field.SetValue(obj, value);
                    return true;
                }

                property = type.GetProperty(fieldName, bindings);
                if (property != null)
                {
                    property.SetValue(obj, value, null);
                    return true;
                }
            }
        }
        return false;
    }

    public static IEnumerable<Type> GetBaseClassesAndInterfaces(this Type type, bool includeSelf = false)
    {
        List<Type> allTypes = new List<Type>();

        if (includeSelf) allTypes.Add(type);

        if (type.BaseType == typeof(object))
        {
            allTypes.AddRange(type.GetInterfaces());
        }
        else
        {
            allTypes.AddRange(
                    Enumerable
                    .Repeat(type.BaseType, 1)
                    .Concat(type.GetInterfaces())
                    .Concat(type.BaseType.GetBaseClassesAndInterfaces())
                    .Distinct());
        }

        return allTypes;
    }
}
#endif