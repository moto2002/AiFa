﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AIBehaviorTree;
using LitJson;
using UnityEditor;
using UnityEngine;

public class NodeEditor : EditorWindow {

    [MenuItem("Window/NodeEditor")]
    static void ShowEditor()
    {
        NodeEditor editor = GetWindow<NodeEditor>();
        editor.Init();   
    }

    const string TREE_OUTPUTPATH = "/AIBehaviorTree/Resources/tree/";
    const string SCRIPT_OUTPUTPATH = "/AIBehaviorTree/Resources/";

    
    private NodeGraph m_EnterNode;
    private void Init()
    {       
        m_EnterNode = new NodeGraph();
        m_EnterNode.ToRect();


        m_EnterNode.Type = NodeGraph.NODETYPE.SEQUENCE;
    }

    #region 菜单功能
    void OnExportAllHandler(object data)
    {
        var node = data as NodeGraph;
        JsonData jd = NodeGraph.CreateNodeJsonData(node);
        var path = Application.dataPath + TREE_OUTPUTPATH + node.OutPutPath +".json";
        File.WriteAllText(path, jd.ToJson());
        EditorUtility.DisplayDialog("提示", "导出成功"+ path, "ok");
        AssetDatabase.Refresh();
    }
    void OnNodeDeleteHandler(object data)
    {
        var node = data as NodeGraph;
        if (node.Parent != null)
            node.Parent.RemoveNode(node);                    
        else
            EditorUtility.DisplayDialog("警告", "不可删除根节点", "ok");
    }

    void OnNodeMenuClickCreateChildHandler(object data)
    {
        var node = data as NodeGraph;
        var child = CreateANewNode( new Vector2(node.NodeRect.x+300, node.NodeRect.y));
        node.AddNode(child);
    }
    void OnNodeMoveUpInParentHandler(object data)
    {
        var node = data as NodeGraph;
        if (node.Parent != null)
        {
            int index = node.Parent.Nodes.IndexOf(node);
            node.Parent.ExchangeChild(index, index - 1);
        }
    }
    void OnNodeMoveDownInParentHandler(object data)
    {
        var node = data as NodeGraph;
        if (node.Parent != null)
        {
            int index = node.Parent.Nodes.IndexOf(node);
            node.Parent.ExchangeChild(index, index + 1);
        }
    }
    #endregion
    NodeGraph CreateANewNode(Vector2 mpos)
    {
        var node = new NodeGraph();
        node.ClickPos = mpos;
        node.ToRect();
        return node;
    }

    NodeGraph GetNodeByID(int id)
    {
        return NodeGraph.FindByID(m_EnterNode,id);
    }
    NodeGraph GetContainMousePosNode(Vector2 mpos)
    {
       return NodeGraph.FindByMousePos(m_EnterNode,mpos);
    }

    private Vector2 WindowScrollPos;
    private NodeGraph RightClickNode = null;
    void OnGUI()
    {
        #region EventHandler
        //right click event
        if (Event.current.type == EventType.ContextClick)
        {
            var menu = new GenericMenu();
            RightClickNode = GetContainMousePosNode(Event.current.mousePosition+ WindowScrollPos);
            if (RightClickNode != null)
            {
                menu.AddItem(new GUIContent("创建子节点"),false, OnNodeMenuClickCreateChildHandler, RightClickNode);                
                if(RightClickNode.Parent!=null)
                {
                    menu.AddItem(new GUIContent("删除当前节点"), false, OnNodeDeleteHandler, RightClickNode);
                    if (RightClickNode.Parent.HasPrevChild(RightClickNode))
                    {
                        menu.AddItem(new GUIContent("向上移动"), false, OnNodeMoveUpInParentHandler, RightClickNode);
                    }
                    if(RightClickNode.Parent.HasNextChild(RightClickNode))
                    {
                        menu.AddItem(new GUIContent("向下移动"), false, OnNodeMoveDownInParentHandler, RightClickNode);
                    }
                }
            }
            menu.ShowAsContext();
            Event.current.Use();
        }
        
        #endregion
        WindowScrollPos = GUI.BeginScrollView(new Rect(0, 0, position.width, position.height),
        WindowScrollPos, new Rect(0, 0, 10000, 10000));


        if (m_EnterNode != null)
        {
            DrawCurvesImpl(m_EnterNode);
            BeginWindows();
            
            InitWindow(m_EnterNode);
            EndWindows();
           
        }

        
        GUI.EndScrollView();  //结束 ScrollView 窗口  
    }

    void InitWindow(NodeGraph parent)
    {
        string title = parent.Parent == null ? "Root" : string.Format("No.{0}", parent.Parent.Nodes.IndexOf(parent));
        GUI.color = parent.GetColorByType();
        
        parent.NodeRect = GUI.Window(parent.ID, parent.NodeRect, DrawNodeWindow, new GUIContent(title));
        GUI.color = Color.white;
        for (int i = 0; i < parent.Nodes.Count; ++i)
        {
            InitWindow(parent.Nodes[i]);
        }
    }

    private TextAsset m_CurrentTextAsset = null;
    void OnSelectionChange()
    {
        if (Selection.objects.Length > 0)
        {
            m_CurrentTextAsset = Selection.objects[0] as TextAsset;
            if (m_CurrentTextAsset == null)
            {
                //EditorUtility.DisplayDialog("格式有误","希望选中的是一个TextAsset","ok");
                return;
            }  
            JsonData jd = null; 
            try
            {
                jd = JsonMapper.ToObject<JsonData>(m_CurrentTextAsset.text);
            }
            catch (Exception e)
            {
                //EditorUtility.DisplayDialog("格式有误", "希望打开的是一个json数据", "ok");
                return;
            }
            m_EnterNode = NodeGraph.CreateNodeGraph(jd);
        }       
    }


    void DrawNodeWindow(int id)
    {
        var node = GetNodeByID(id);
        if (node == null)
            return;

        EditorGUILayout.BeginVertical("box");
        node.Name = EditorGUILayout.TextField("Name", node.Name);
        node.Type = (NodeGraph.NODETYPE)EditorGUILayout.EnumPopup("Type", node.Type);

        EditorGUILayout.BeginVertical("box");
        node.ScriptName = EditorGUILayout.TextField("ScriptName", node.ScriptName);
        if(node.ScriptName != "")
        {
            string fullPath = Application.dataPath + SCRIPT_OUTPUTPATH + node.ScriptName + ".txt";
            if (!File.Exists(fullPath))
            {
                if (GUILayout.Button("Create Script"))
                    NodeScriptTemplate.NewEmptyScript(fullPath, node);
            }
            else 
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Delete Script"))
                    NodeScriptTemplate.DeleteScript(fullPath);
                if (GUILayout.Button("Edit Script"))
                    System.Diagnostics.Process.Start(fullPath);
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndVertical();
        if (node.Parent != null)
        {
            if (node.Parent.Type == NodeGraph.NODETYPE.RANDOWSELECT)
                node.Weight = EditorGUILayout.IntField("Weight *(Weight>0)", Mathf.Max(1, node.Weight));
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        node.ToolBarSelectIndex = GUILayout.Toolbar(node.ToolBarSelectIndex, NodeGraph.ToolBarNames);
        if(node.ToolBarSelectIndex == 0)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.HelpBox("导出当前节点为根节点的数据,导出格式xxx", MessageType.Info);
            node.OutPutPath = EditorGUILayout.TextField("", node.OutPutPath);
            if (node.OutPutPath != "" && GUILayout.Button("导出文件"))
            {
                OnExportAllHandler(node);
            }
            EditorGUILayout.EndVertical();
        }
        else if(node.ToolBarSelectIndex == 1)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.HelpBox("设置子树", MessageType.Info);
            node.SubTreeAsset = EditorGUILayout.ObjectField("SubTree TextAsset", node.SubTreeAsset, typeof(TextAsset)) as TextAsset;
            if (node.SubTreeAsset != null && GUILayout.Button("Add SubTree"))
            {
                node.AddNode(NodeGraph.CreateNodeGraph(JsonMapper.ToObject<JsonData>(node.SubTreeAsset.text)), new Vector2(200, 0));
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();
        GUI.DragWindow(new Rect(0, 0, 1000,20));
    }


    void DrawCurvesImpl(NodeGraph parent)
    {
        for (int i = 0; i < parent.Nodes.Count; ++i)
        {
            DrawNodeCurve(parent.NodeRect,parent.Nodes[i].NodeRect);
            DrawCurvesImpl(parent.Nodes[i]);
        }
    }
    void DrawNodeCurve(Rect start, Rect end)
    {       
        Vector3 startPos = new Vector3(start.x + start.width, start.y + start.height / 2, 0);
        Vector3 endPos = new Vector3(end.x, end.y + end.height / 2, 0);
        Vector3 startTan = startPos + Vector3.right * 40;
        Vector3 endTan = endPos + Vector3.left * 40;
        Color shadowCol = new Color(0f, 0f, 0f, 0.06f);
        for (int i = 0; i < 3; i++) // Draw a shadow
            Handles.DrawBezier(startPos, endPos, startTan, endTan, shadowCol, null, (i + 1) * 5);
        Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.black, null, 2);
    }
}
