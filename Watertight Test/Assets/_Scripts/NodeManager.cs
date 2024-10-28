using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

#endif

public class NodeManager : MonoBehaviour
{


    [SerializeReference] private Node node;

    [System.Serializable]
     public class Node
    {
        //TODO - Simplify. Just use (Node,bool) tuples for directions, then pass as ref.
        public enum VerticalBlocker
        {
            CEILING,
            FLOOR,
            NONE
        }

        public enum HorizontalBlocker
        {
            DOOR,
            WALL,
            NONE
        }
        [SerializeField] private string id;
        [SerializeField] private VerticalBlocker topBlocker;
        public VerticalBlocker TopBlocker => topBlocker;
        [SerializeField] private VerticalBlocker bottomBlocker;
        public VerticalBlocker BottomBlocker => bottomBlocker;
        [SerializeField] private HorizontalBlocker northBlocker;
        public HorizontalBlocker NorthBlocker => northBlocker;
        [SerializeField] private HorizontalBlocker eastBlocker;
        public HorizontalBlocker EastBlocker => eastBlocker;
        [SerializeField] private HorizontalBlocker southBlocker;
        public HorizontalBlocker SouthBlocker => southBlocker;
        [SerializeField] private HorizontalBlocker westBlocker;
        public HorizontalBlocker WestBlocker => westBlocker;

        [SerializeReference] private Node topNode;
        public Node TopNode => topNode;
        [SerializeReference] private Node bottomNode;
        public Node BottomNode => bottomNode;
        [SerializeReference] private Node northNode;
        public Node NorthNode => northNode;
        [SerializeReference] private Node eastNode;
        public Node EastNode => eastNode;
        [SerializeReference] private Node southNode;
        public Node SouthNode => southNode;
        [SerializeReference] private Node westNode;
        public Node WestNode => westNode;

        public void SetTopNode(Node node)
        {
            topNode = node;
        }
        public void SetBottomNode(Node node)
        {
            bottomNode = node;
        }
        public void SetNorthNode(Node node)
        {
            northNode = node;
        }
        public void SetEastNode(Node node)
        {
            eastNode = node;
        }
        public void SetSouthNode(Node node)
        {
            southNode = node;
        }
        public void SetWestNode(Node node)
        {
            westNode = node;
        }
    }

    private void OnDrawGizmos()
    {
        HashSet<Node> drawnNodes = new HashSet<Node>();

        float alpha = 1.0f;
        void DrawNode(Node node, Vector3 position)
        {
            if (node != null && !drawnNodes.Contains(node))
            {
                drawnNodes.Add(node);
                SortedGizmos.color = Color.grey;
                SortedGizmos.DrawSphere(position, 0.1f);
                if (node.TopBlocker != Node.VerticalBlocker.NONE)
                {
                    SortedGizmos.color = new Color(0,1,0, alpha);
                    SortedGizmos.DrawCube(position + Vector3.up * 0.45f, new Vector3(1, 0.1f, 1));
                }
                if (node.BottomBlocker != Node.VerticalBlocker.NONE)
                {
                    SortedGizmos.color = new Color(0, 0.5f, 0, alpha);
                    SortedGizmos.DrawCube(position + Vector3.down * 0.45f, new Vector3(1, 0.1f, 1));
                }
                if (node.NorthBlocker != Node.HorizontalBlocker.NONE)
                {
                    SortedGizmos.color = new Color(0, 0, 1.0f, alpha);
                    SortedGizmos.DrawCube(position + Vector3.forward * 0.45f, new Vector3(1, 1, 0.1f));
                }
                if (node.EastBlocker != Node.HorizontalBlocker.NONE)
                {
                    SortedGizmos.color = new Color(1.0f, 0, 0, alpha);
                    SortedGizmos.DrawCube(position + Vector3.right * 0.45f, new Vector3(0.1f, 1, 1));
                }
                if (node.SouthBlocker != Node.HorizontalBlocker.NONE)
                {
                    SortedGizmos.color = new Color(0, 0, 0.5f, alpha);
                    SortedGizmos.DrawCube(position + Vector3.back * 0.45f, new Vector3(1, 1, 0.1f));
                }
                if (node.WestBlocker != Node.HorizontalBlocker.NONE)
                {
                    SortedGizmos.color = new Color(0.5f, 0, 0, alpha);
                    SortedGizmos.DrawCube(position + Vector3.left * 0.45f, new Vector3(0.1f, 1, 1));
                }
                DrawNode(node.TopNode, position + Vector3.up);
                DrawNode(node.BottomNode, position + Vector3.down);
                DrawNode(node.NorthNode, position + Vector3.forward);
                DrawNode(node.EastNode, position + Vector3.right);
                DrawNode(node.SouthNode, position + Vector3.back);
                DrawNode(node.WestNode, position + Vector3.left);

            }
        }

        DrawNode(node, Vector3.zero);

        SortedGizmos.BatchCommit();
    }

    [NaughtyAttributes.Button]
    private void CheckWaterTight() //Definately better ways to do this - JM
    {
        HashSet<Node> confirmedUnsafeNodes = new HashSet<Node>();
        HashSet<Node> unCheckedNodes = new HashSet<Node>();//nodes that are adjacent but are blocked by walls.
        HashSet<Node> checkedNodes = new HashSet<Node>();
        bool IsSafe(Node node)
        {
            if(confirmedUnsafeNodes.Contains(node))
            {
                return false;
            }
            else if(checkedNodes.Contains(node))
            {
                return true;
            }
            if (unCheckedNodes.Contains(node))
            {
                unCheckedNodes.Remove(node);
            }
            if (!checkedNodes.Contains(node))
            {
                checkedNodes.Add(node);
            }
            bool topSafe = false;
            bool bottomSafe = false;
            bool northSafe = false;
            bool eastSafe = false;
            bool southSafe = false;
            bool westSafe = false;
            //top
            if (node.TopBlocker != Node.VerticalBlocker.NONE || (node.TopNode != null && node.TopNode.BottomBlocker != Node.VerticalBlocker.NONE))
            {
                topSafe = true;
                if (!checkedNodes.Contains(node.TopNode) && node.TopNode != null)
                {
                    unCheckedNodes.Add(node.TopNode);
                }
            }
            else if(node.TopNode != null)
            {
                topSafe = IsSafe(node.TopNode);
            }
            else
            {
                topSafe = false;
            }
            if(!topSafe)
            {
                confirmedUnsafeNodes.Add(node);
            }
            //bottom
            if (node.BottomBlocker != Node.VerticalBlocker.NONE || (node.BottomNode != null && node.BottomNode.TopBlocker != Node.VerticalBlocker.NONE))
            {
                bottomSafe = true;
                if (!checkedNodes.Contains(node.BottomNode) && node.BottomNode != null)
                {
                    unCheckedNodes.Add(node.BottomNode);
                }
            }
            else if (node.BottomNode != null)
            {
                bottomSafe = IsSafe(node.BottomNode);
            }
            else
            {
                bottomSafe = false;
            }
            if (!bottomSafe)
            {
                confirmedUnsafeNodes.Add(node);
            }
            //north
            if (node.NorthBlocker != Node.HorizontalBlocker.NONE || (node.NorthNode != null && node.NorthNode.SouthBlocker != Node.HorizontalBlocker.NONE))
            {
                northSafe = true;
                if (!checkedNodes.Contains(node.NorthNode) && node.NorthNode != null)
                {
                    unCheckedNodes.Add(node.NorthNode);
                }
            }
            else if (node.NorthNode != null)
            {
                northSafe = IsSafe(node.NorthNode);
            }
            else
            {
                northSafe = false;
            }
            if (!northSafe)
            {
                confirmedUnsafeNodes.Add(node);
            }
            //east
            if (node.EastBlocker != Node.HorizontalBlocker.NONE || (node.EastNode != null && node.EastNode.WestBlocker != Node.HorizontalBlocker.NONE))
            {
                if (!checkedNodes.Contains(node.EastNode) && node.EastNode != null)
                {
                    unCheckedNodes.Add(node.EastNode);
                }
                eastSafe = true;
            }
            else if (node.EastNode != null)
            {
                eastSafe = IsSafe(node.EastNode);
            }
            else
            {
                eastSafe = false;
            }
            if (!eastSafe)
            {
                confirmedUnsafeNodes.Add(node);
            }
            //south
            if (node.SouthBlocker != Node.HorizontalBlocker.NONE || (node.SouthNode != null && node.SouthNode.NorthBlocker != Node.HorizontalBlocker.NONE))
            {
                if (!checkedNodes.Contains(node.SouthNode) && node.SouthNode != null)
                {
                    unCheckedNodes.Add(node.SouthNode);
                }
                southSafe = true;
            }
            else if (node.SouthNode != null)
            {
                southSafe = IsSafe(node.SouthNode);
            }
            else
            {
                confirmedUnsafeNodes.Add(node);
                southSafe = false;
            }
            if (!southSafe)
            {
                confirmedUnsafeNodes.Add(node);
            }
            //west
            if (node.WestBlocker != Node.HorizontalBlocker.NONE || (node.WestNode != null && node.WestNode.EastBlocker != Node.HorizontalBlocker.NONE))
            {
                if (!checkedNodes.Contains(node.WestNode) && node.WestNode != null )
                {
                    unCheckedNodes.Add(node.WestNode);
                }
                westSafe = true;
            }
            else if (node.WestNode != null)
            {
                westSafe = IsSafe(node.WestNode);
            }
            else
            {
                confirmedUnsafeNodes.Add(node);
                westSafe = false;
            }
            if (!westSafe)
            {
                confirmedUnsafeNodes.Add(node);
            }
            return topSafe && bottomSafe && northSafe && eastSafe && southSafe && westSafe;
        }

        ValidateTree();

        unCheckedNodes.Add(node);
        while(unCheckedNodes.Count > 0)
        {
            //tally newly safe nodes from here to create chains of rooms.
            Debug.Log(IsSafe(unCheckedNodes.First()));
        }
        
    }

    [NaughtyAttributes.Button]
    private void ValidateTree()
    {
        Dictionary<Vector3, Node> allNodes = new Dictionary<Vector3, Node>();
        HashSet<Node> collectedNodes = new HashSet<Node>();

        void CollectNode(Node node, Vector3 position)
        {
            if (node == null || collectedNodes.Contains(node))
            {
                return;
            }
            if (allNodes.ContainsKey(position))
            {
                Debug.LogError("Overlapping Nodes");
                return;
            }

            allNodes.Add(position, node);
            collectedNodes.Add(node);
            CollectNode(node?.TopNode, position + Vector3.up);
            CollectNode(node?.BottomNode, position + Vector3.down);
            CollectNode(node?.NorthNode, position + Vector3.forward);
            CollectNode(node?.EastNode, position + Vector3.right);
            CollectNode(node?.SouthNode, position + Vector3.back);
            CollectNode(node?.WestNode, position + Vector3.left);
        }

        CollectNode(node, Vector3.zero);

        foreach(var pair in allNodes)
        {
            var position = pair.Key;
            var currentNode = pair.Value;
            if(allNodes.ContainsKey(position + Vector3.up))
            {
                currentNode.SetTopNode(allNodes[position + Vector3.up]);
            }
            if (allNodes.ContainsKey(position + Vector3.down))
            {
                currentNode.SetBottomNode(allNodes[position + Vector3.down]);
            }
            if (allNodes.ContainsKey(position + Vector3.forward))
            {
                currentNode.SetNorthNode(allNodes[position + Vector3.forward]);
            }
            if (allNodes.ContainsKey(position + Vector3.right))
            {
                currentNode.SetEastNode(allNodes[position + Vector3.right]);
            }
            if (allNodes.ContainsKey(position + Vector3.back))
            {
                currentNode.SetSouthNode(allNodes[position + Vector3.back]);
            }
            if (allNodes.ContainsKey(position + Vector3.left))
            {
                currentNode.SetWestNode(allNodes[position + Vector3.left]);
            }
        }

    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(Node), true)]
    public class NodePropertyDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect labelRect = new Rect(position.min.x, position.min.y, position.size.x, EditorGUIUtility.singleLineHeight);
            float height = EditorGUIUtility.singleLineHeight;
            if (property.managedReferenceValue != null)
            {
                property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label);
                if (property.isExpanded)
                {
                    EditorGUI.indentLevel++;
                   
                    string[] ignoredNames = new string[] { "type" };
                    foreach (var child in property.GetChildren())
                    {
                        if (!ignoredNames.Contains(child.name))
                        {
                            float childHeight = EditorGUI.GetPropertyHeight(child, true);
                            Rect rect = new Rect(position.min.x, position.min.y + height, position.size.x, EditorGUIUtility.singleLineHeight);
                            if (childHeight == 0)
                            {
                                height += EditorGUIUtility.singleLineHeight;
                            }
                            height += childHeight;
                            EditorGUI.PropertyField(rect, child, true);
                        }
                    }
                    Rect deleteRect = new Rect(position.min.x, position.min.y + height, position.size.x, EditorGUIUtility.singleLineHeight);
                    deleteRect = EditorGUI.IndentedRect(deleteRect);
                    height += EditorGUIUtility.singleLineHeight;
                    bool buttonPressed = GUI.Button(deleteRect, "Remove " + property.displayName);
                    if (buttonPressed)
                    {
                        property.managedReferenceValue = null;
                    }
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUI.LabelField(labelRect, property.displayName);
                Rect buttonRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.min.y, position.size.x - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
                bool buttonPressed = GUI.Button(buttonRect, "Add " + property.displayName);
                if (buttonPressed)
                {
                    property.managedReferenceValue = new Node();
                }
            }
            property.serializedObject.ApplyModifiedProperties();
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (property.isExpanded && property.managedReferenceValue != null)
            {
                height += EditorGUIUtility.singleLineHeight;
                foreach (var child in property.GetChildren())
                {
                    float childHeight = EditorGUI.GetPropertyHeight(child, true);
                    height += childHeight;
                }

            }
            return height;
        }
    }
#endif
}
