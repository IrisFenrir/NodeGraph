# 节点编辑器
![[https://github.com/IrisFenrir/NodeGraph/blob/main/Pasted%20image%2020211205170211.png]]
使用该框架，您可以实现形如上图的效果。
这个节点编辑器只提供创建节点、删除节点、节点连线、显示节点属性的功能，具体的逻辑需要由其他类来实现。节点编辑器只负责提供节点间的连接关系，您需要在连接好节点后，在外部读取连接关系去创建自己的树形结构。比如创建行为树和状态机。

## 一、如何使用
### 1. 一些固定的流程
#### (1) 创建ScriptableObject
接下来将以行为树为例，讲解如何使用本框架。
以下步骤为固定的流程，使用时只需修改类名即可。
首先您需要创建一个新文件夹来存放将要实现的模块。比如行为树模块命名为BehaviourTree。
然后创建Editor文件夹存放编辑器扩展相关的脚本，创建Runtime文件夹存放运行时要调用的脚本。
在Runtime文件夹下，创建一个存放数据的类继承NodeGraphData。NodeGraphData是存放了节点和连边数据的ScriptableObject。这里我们创建BehaviourTreeData继承NodeGraphData，除继承外，不用添加额外的内容。

```csharp
using IrisFenrir.NodeGraphTools;

namespace IrisFenrir.AI.BehaviourTree
{
    public class BehaviourTreeData : NodeGraphData
    {
        
    }
}
```

然后您需要在Editor文件下，创建一个继承EndNameEditAction的类，这个在创建ScriptableObject对象时使用。这里创建BehaviourTreeCreator类。
```csharp
using UnityEditor;
using UnityEditor.ProjectWindowCallback;

namespace IrisFenrir.AI.BehaviourTree
{
    public class BehaviourTreeCreator : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            BehaviourTreeData data = CreateInstance<BehaviourTreeData>();
            AssetDatabase.CreateAsset(data, pathName);
            Selection.activeObject = data;
        }
    }
}

```

接下来创建一个菜单类，来负责实现在Project面板右键能创建出刚刚写的BehaviourTreeData。
```csharp
using UnityEditor;
using UnityEngine;

namespace IrisFenrir.AI.BehaviourTree
{
    public class BehaviourTreeMenu
    {
        [MenuItem("Assets/Create/Custom/Behaviour Tree")]
        private static void CreateNodeGraphData()
        {
            var creator = ScriptableObject.CreateInstance<BehaviourTreeCreator>();

            string fileName = "New Behaviour Tree.asset";

            GUIContent content = EditorGUIUtility.IconContent("d_ScriptableObject Icon");

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(creator.GetInstanceID(), creator,
                fileName, (Texture2D)content.image, null);
        }
    }
}

```
实现以上三个类后，我们可以在Project菜单找到Behaviour Tree，并创建出来。
![[Pasted image 20211205172027.png]]
然后你会得到一个这样的ScriptableObject对象，当然里面还没有数据。
![[Pasted image 20211205172122.png]]

#### (2) 显示编辑器界面
首先要通过继承ToolbarLayer实现自定义的工具栏。
ToolbarLayer是工具栏层，继承自GraphLayer。
你可以重写ToolbarLayer的一些方法来定制自己的工具栏。
基础的方法有：
-  void Init()
在Draw之前执行一次。可以实现一些初始化的内容。重写时请保留base.Init()。
- void Draw()
所有绘制功能都在此实现。基类会在此绘制工具栏。通常不需要修改这里。
- void ProcessEvent(Event e)
所有需要事件的功能在此实现，如鼠标点击事件。在打开窗口会，会将e设为Event.current。
- void AddItem(string name, float width, ToolbarAction callback)
可以使用此函数添加工具栏菜单选项，设置选项名name、宽度width和点击后执行的功能callback。ToolbarAction是一个返回int类型的无参委托。返回值代表当前选中了第几个选项，返回-1则表示没有选择。

另外你可以访问的字段有：
- int menuSelected
表示当前选中了菜单的哪一项。-1表示没有选中。
- Color backgroundColor
工具栏的背景颜色。默认为(0.2f, 0.2f, 0.2f)。
- Rect menuItemArea
当前工具栏选项的区域，你可以用这个判断鼠标是否点在菜单区，但不要去修改它的值，基类会去维护它。
- List\<MenuItem\> menuItems
MenuItem里存了选项名name、宽度width、回调函数callback以及该选项的区域rect。
- Color normalTextColor
选项未被选中时的字体颜色，默认为Color.white。
- Color hoverTextColor
鼠标悬停时选项字体的颜色，默认为(18, 183, 245, 255) / 255f。

接下来实现行为树的工具栏BehaviourTreeToolbarLayer。
```csharp
using IrisFenrir.NodeGraphTools;
using UnityEditor;
using UnityEngine;

namespace IrisFenrir.AI.BehaviourTree
{
    public class BehaviourTreeToolLayer : ToolbarLayer
    {
        private Rect m_sourceArea;
        private GUIStyle m_sourceStyle;
        private Rect m_sourceDataLabelArea;
        private GUIStyle m_sourceLabelStyle;
        private Rect m_sourceDataArea;
        private Rect m_layoutTypeLabelArea;
        private Rect m_layoutTypeArea;

        public BehaviourTreeToolLayer(EditorWindow window, Rect rect, int priority = 2) :
            base(window, rect, priority)
        {
			// 设置背景颜色
            backgroundColor = new Color(0.2f, 0.2f, 0.2f);
			
			// 添加两个菜单选项
            AddItem("Source", 50f, OnClickSource);
            AddItem("Save", 50f, OnClickSave);
			
			// 为Source面板设置区域和样式
            m_sourceArea = new Rect(0, 20, 300, 100);
            m_sourceStyle = new GUIStyle()
            {
                normal =
                {
                    background = GraphGlobalSetting.NodeTexture,
                    textColor = Color.white,
                },
                border = new RectOffset(10, 10, 10, 10),
                clipping = TextClipping.Clip
            };
            m_sourceDataLabelArea = new Rect(10, 40, 50, 15);
            m_sourceLabelStyle = GUI.skin.label;
            m_sourceLabelStyle.fontStyle = FontStyle.Bold;
            m_sourceDataArea = new Rect(70, 40, 220, 15);

            m_layoutTypeLabelArea = new Rect(10, 60, 50, 15);
            m_layoutTypeArea = new Rect(70, 60, 220, 15);
        }
		
		// 点击Source选项时，显示下面的内容
		// 其中Context是储存全局变量的对象，内部的Data是前面创建的ScriptableObject对象
        private int OnClickSource()
        {
            GUI.Box(m_sourceArea, string.Empty, m_sourceStyle);

            GUI.BeginGroup(m_sourceArea);
            GUI.Label(m_sourceDataLabelArea, "Source", m_sourceLabelStyle);
            Context.Data = EditorGUI.ObjectField(m_sourceDataArea, Context.Data, typeof(BehaviourTreeData), false) as BehaviourTreeData;

            GUI.Label(m_layoutTypeLabelArea, "Layout", m_sourceLabelStyle);

            Context.layoutType = (GraphBaseNode.PortLayout)EditorGUI.EnumPopup(m_layoutTypeArea, Context.layoutType);
            
            GUI.EndGroup();
			
			// 返回0，因为Source是第0个选项
            return 0;
        }

        private int OnClickSave()
        {
			// 调用Data的保存方法
            Context.Data.Save();
			// 返回-1代表不选中任何选项
            return -1;
        }

        public override void ProcessEvent(Event e)
        {
            base.ProcessEvent(e);

            // 关闭Source窗口
			// 如果点到其他地方则关闭窗口
            if (!m_sourceArea.Contains(e.mousePosition) &&
                !menuItems[0].rect.Contains(e.mousePosition) &&
                e.type == EventType.MouseDown)
            {
                menuSelected = -1;
            }
        }
    }
}

```

![[Pasted image 20211205181106.png]]

接下来重写背景层，因为我们要使用TreeView去创建各种节点，而默认是使用GenericMenu去显示的。
创建BehaviourTreeBackgroundLayer，继承BackgroundLayer。
可以使用的方法除了与前面相同的Init、Draw和ProcessEvent外，还有CreateMenu(Event e)，我们将重写这个方法去创建TreeView。TreeView的详细介绍请参考官方文档。

```csharp
using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace IrisFenrir.AI.BehaviourTree
{
    public class BehaviourTreeView : TreeView
    {
        private TreeViewItem m_root;
        private TreeViewItem m_rootNode;
        private TreeViewItem m_compositeNode;
        private TreeViewItem m_decoratorNode;
        private TreeViewItem m_conditionNode;
        private TreeViewItem m_actionNode;
        private List<TreeViewItem> m_items;
        private int m_currentID;

        public Action<string> onDoubleClick;

        public BehaviourTreeView(TreeViewState state):base(state)
        {
            showAlternatingRowBackgrounds = true;
            showBorder = true;

            m_root = new TreeViewItem() { id = 0, depth = -1, displayName = "Root" };

            m_rootNode = new TreeViewItem() { id = 1, depth = 0, displayName = "Root" };
            m_compositeNode = new TreeViewItem() { id = 2, depth = 0, displayName = "Composite" };
            m_decoratorNode = new TreeViewItem() { id = 3, depth = 0, displayName = "Decorator" };
            m_conditionNode = new TreeViewItem() { id = 4, depth = 0, displayName = "Condition" };
            m_actionNode = new TreeViewItem() { id = 5, depth = 0, displayName = "Action" };

            m_currentID = 6;

            m_items = new List<TreeViewItem>();

            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            m_items.Add(m_rootNode);
            m_items.Add(m_compositeNode);
            m_items.Add(m_decoratorNode);
            m_items.Add(m_conditionNode);
            m_items.Add(m_actionNode);

            SetupParentsAndChildrenFromDepths(m_root, m_items);

            return m_root;
        }

        protected override void DoubleClickedItem(int id)
        {
            if(onDoubleClick != null)
            {
                onDoubleClick(FindItem(id));
            }
        }

        private string FindItem(int id)
        {
            if(m_rootNode.id == id)
            {
                return m_rootNode.displayName;
            }
            if(m_compositeNode.children != null)
            {
                var item = m_compositeNode.children.Find(x => x.id == id);
                if(item != null)
                {
                    return item.displayName;
                }
            }
            if (m_decoratorNode.children != null)
            {
                var item = m_decoratorNode.children.Find(x => x.id == id);
                if (item != null)
                {
                    return item.displayName;
                }
            }
            if (m_conditionNode.children != null)
            {
                var item = m_conditionNode.children.Find(x => x.id == id);
                if (item != null)
                {
                    return item.displayName;
                }
            }
            if (m_actionNode.children != null)
            {
                var item = m_actionNode.children.Find(x => x.id == id);
                if (item != null)
                {
                    return item.displayName;
                }
            }
            return string.Empty;
        }

        public void AddCompositeNode(string name)
        {
            m_compositeNode.AddChild(new TreeViewItem() { id = m_currentID++, depth = 1, displayName = name });
        }
        public void AddDecoratorNode(string name)
        {
            m_decoratorNode.AddChild(new TreeViewItem() { id = m_currentID++, depth = 1, displayName = name });
        }
        public void AddConditionNode(string name)
        {
            m_conditionNode.AddChild(new TreeViewItem() { id = m_currentID++, depth = 1, displayName = name });
        }
        public void AddActionNode(string name)
        {
            m_actionNode.AddChild(new TreeViewItem() { id = m_currentID++, depth = 1, displayName = name });
        }
    }
}

```

```csharp
using System.Reflection;
using IrisFenrir.NodeGraphTools;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace IrisFenrir.AI.BehaviourTree
{
    public class BehaviourTreeBackgroundLayer : BackgroundLayer
    {
        private bool m_drawSearch;
        private Rect m_searchArea;
        private Rect m_searchFieldArea;
        private Rect m_treeViewArea;
        private SearchField m_searchField;
        private TreeViewState m_treeViewState;
        private BehaviourTreeView m_treeView;

        public BehaviourTreeBackgroundLayer(EditorWindow window, Rect rect, int priority = 0) :
            base(window, rect, priority)
        {
            m_searchArea.size = new Vector2(200, 300);
            m_searchFieldArea.size = new Vector2(200, 20);
            m_treeViewArea.size = new Vector2(200, 270);

            m_treeViewState = new TreeViewState();
            m_searchField = new SearchField();
            m_treeView = new BehaviourTreeView(m_treeViewState);
            m_searchField.downOrUpArrowKeyPressed += m_treeView.SetFocusAndEnsureSelectedItem;
            m_treeView.onDoubleClick += CloseSearch;

			// 在这里为Tree手动添加节点
            m_treeView.AddCompositeNode("Sequence");
            m_treeView.AddCompositeNode("Selector");

            m_treeView.AddDecoratorNode("Invertor");

            m_treeView.AddConditionNode("IntoRange");

            m_treeView.AddActionNode("Debug");
            m_treeView.AddActionNode("Patrol");
        }

        public override void Draw()
        {
            base.Draw();

            if (m_drawSearch)
            {
                DrawSearch();
            }
        }

        public override void ProcessEvent(Event e)
        {
            base.ProcessEvent(e);

            if (e.type == EventType.MouseDown && e.button == 0 &&
                !m_searchArea.Contains(e.mousePosition))
            {
                m_drawSearch = false;
            }
        }

        protected override void CreateMenu(Event e)
        {
            m_drawSearch = true;
            m_searchArea.position = e.mousePosition + Vector2.up * 10;
        }

        private void DrawSearch()
        {
            EditorGUI.DrawRect(m_searchArea, Color.black);

            m_searchFieldArea.position = m_searchArea.position;
            
            m_treeView.searchString = m_searchField.OnGUI(m_searchFieldArea, m_treeView.searchString);
            m_treeViewArea.position = m_searchFieldArea.position + Vector2.up * 20;
            
            m_treeView.OnGUI(m_treeViewArea);
        }

        private void CloseSearch(string nodeName)
        {
            m_drawSearch = false;
            
			// 使用反射创建节点，具体请根据自己的程序集和命名空间名字进行修改
			// 节点类的格式统一为xxxGraphNode，其中xxx是在TreeView中显示的名字
            var type = Assembly.Load("Assembly-CSharp").GetType("IrisFenrir.AI.BehaviourTree." + nodeName + "GraphNode");
            if(type != null)
            {
                Context.CreateNodeOfType(type);
            }
            
        }
    }
}

```

完成工具层和背景层后，写一个Graph将它们组合起来。
创建BehaviourTreeGraph类，继承NodeGraph。

```csharp

using IrisFenrir.NodeGraphTools;
using UnityEditor;
using UnityEngine;

namespace IrisFenrir.AI.BehaviourTree
{
    public class BehaviourTreeGraph : NodeGraph
    {
        // 背景
        private Rect m_backgroundArea = new Rect(0, 0, 1, 1);
        private Color m_backgroundColor = new Color(0.1f, 0.1f, 0.1f);
        // 网格
        private Color m_gridLineColor1 = new Color(0.3f, 0.3f, 0.3f);
        private Color m_gridLineColor2 = new Color(0.6f, 0.6f, 0.6f);
        private Color m_gridLineColor3 = new Color(0.8f, 0.8f, 0.8f);
        private float m_gridSpacing = 20f;

        // 工具栏
        private Rect m_toolbarArea = new Rect(0, 0, 1, 20f);

        // 调试信息
        private Rect m_debugArea = new Rect(200, 20, 200, 200);

        private BehaviourTreeBackgroundLayer m_backgroundLayer;
        private BehaviourTreeToolLayer m_toolbalLayer;
        private NodeLayer m_nodeLayer;
        private DebugLayer m_debugLayer;
        private TransitionLayer m_transitionLayer;

        public BehaviourTreeGraph(EditorWindow window):base(window)
        {
            context = new GraphContext();
            m_backgroundLayer = new BehaviourTreeBackgroundLayer(window, m_backgroundArea, 0);

            m_backgroundLayer.AddElement(new GraphBackground(window, m_backgroundArea, m_backgroundColor));
            m_backgroundLayer.AddElement(new GraphGrid(window, m_backgroundArea, m_gridLineColor1,
                m_gridLineColor2, m_gridLineColor3, m_gridSpacing));

            m_toolbalLayer = new BehaviourTreeToolLayer(window, m_toolbarArea, 3);

            m_nodeLayer = new NodeLayer(window, m_backgroundArea, 2);

            m_debugLayer = new DebugLayer(window, m_debugArea, 4);

            m_transitionLayer = new TransitionLayer(window, m_backgroundArea, 1);

            AddElement(m_backgroundLayer);
            AddElement(m_toolbalLayer);
            AddElement(m_nodeLayer);
            AddElement(m_transitionLayer);
            AddElement(m_debugLayer);
        }
    }
}
```

创建一个窗口，继承EditorWindow，去显示Graph。

```csharp
using UnityEditor;
using UnityEngine;

namespace IrisFenrir.AI.BehaviourTree
{
    public class BehaviourTreeWindow : EditorWindow
    {
        private BehaviourTreeGraph m_graph;

        [MenuItem("Window/Custom/Behaviour Tree")]
        private static void OpenWindow()
        {
            GetWindow<BehaviourTreeWindow>("Behaviour Tree");
        }

        private void OnGUI()
        {
            InitGraph();
            m_graph.ProcessEvent(Event.current);
            m_graph.Draw();
        }

        private void OnDisable()
        {
            m_graph?.Save();
        }

        private void InitGraph()
        {
            if (m_graph == null)
            {
                m_graph = new BehaviourTreeGraph(this);
            }
        }
    }
}

```

到这里就走完基本的流程，可以开始创建节点了。

### 2.创建节点
所有要创建的节点都继承GraphBaseNode。
可以使用的属性有：
```csharp
public Rect rect;  // 位置
public string name = "Node";  // 显示在上方的节点名
public Color borderColor;  // 节点外框的颜色
public int id;  // 节点id
public List<int> imports = new List<int>();  // 入边集合
public List<int> outports = new List<int>();  // 出边集合
public bool isDeleted = false;  // 是否被删除
public PortLayout portType = PortLayout.Left2Right;  // 端口布局类型
public List<Property> properties = new List<Property>(); // 属性集合
public int maxImport = -1; // 最大出边数量
public int maxOutport = -1; // 最小入边数量
public bool isDrawProperty = false; // 是否绘制属性
```
可以使用的方法有：
```csharp
public void AddProperty(PropertyType type,string name)
public void AddProperty(PropertyType type,string name,Type objType)
```
添加类类型、列表类型请使用第二个重载。所有可添加的类型已在枚举中列出。

实现BehaviourTree节点。
组合节点基类：
```csharp
using IrisFenrir.NodeGraphTools;
using UnityEngine;

namespace IrisFenrir.AI.BehaviourTree
{
    public abstract class CompositeGraphNode : GraphBaseNode
    {
        public CompositeGraphNode()
        {
			// 节点为绿色
            borderColor = new Color(0, 1, 0, 0.5f);
            name = "Composite";
			// 只允许最多1条入边
			// 出边数量不限制
            maxImport = 1;
        }
    }
}

```
装饰节点基类：
```csharp
using IrisFenrir.NodeGraphTools;
using UnityEngine;

namespace IrisFenrir.AI.BehaviourTree
{
    public abstract class DecoratorGraphNode : GraphBaseNode
    {
        public DecoratorGraphNode()
        {
            borderColor = new Color(254, 215, 88, 128) / 255f;
            name = "Decorator";
            maxImport = 1;
            maxOutport = 1;
        }
    }
}

```
条件节点基类：
```csharp
using IrisFenrir.NodeGraphTools;
using UnityEngine;

namespace IrisFenrir.AI.BehaviourTree
{
    public abstract class ConditionGraphNode : GraphBaseNode
    {
        public ConditionGraphNode()
        {
            borderColor = new Color(1, 0, 1, 0.5f);
            name = "Condition";
            maxImport = 1;
            maxOutport = 0;
        }
    }
}

```
行为节点基类：
```csharp
using IrisFenrir.NodeGraphTools;
using UnityEngine;

namespace IrisFenrir.AI.BehaviourTree
{
    public abstract class ActionGraphNode : GraphBaseNode
    {
        public ActionGraphNode()
        {
            borderColor = new Color(0, 122, 204, 128) / 255f;
            name = "Action";
            maxImport = 1;
            maxOutport = 0;
        }
    }
}

```

接下来实现一些具体的节点。
调试节点。
```csharp
namespace IrisFenrir.AI.BehaviourTree
{
    public class DebugGraphNode : ActionGraphNode
    {
        public DebugGraphNode()
        {
            name = "Debug";

            AddProperty(PropertyType.String, "content");
        }
    }
}

```
巡逻节点：
```csharp
using UnityEngine;

namespace IrisFenrir.AI.BehaviourTree
{
    public class PatrolGraphNode : ActionGraphNode
    {
        public PatrolGraphNode()
        {
            name = "Patrol";

            AddProperty(PropertyType.List, "path", typeof(Transform));
        }
    }
}

```
序列节点：
```csharp
namespace IrisFenrir.AI.BehaviourTree
{
    public class SequenceGraphNode : CompositeGraphNode
    {
        public SequenceGraphNode()
        {
            name = "Sequence";
        }
    }
}

```
范围节点：
```csharp
namespace IrisFenrir.AI.BehaviourTree
{
    public class IntoRangeGraphNode : ConditionGraphNode
    {
        public IntoRangeGraphNode()
        {
            name = "IntoRange";

            AddProperty(PropertyType.Tag, "tag");
            AddProperty(PropertyType.Layer, "layer");
            AddProperty(PropertyType.Float, "range");
        }
    }
}

```

实现一系列节点后，别忘了去BehaviourTreeBackgroundLayer里添加选项。最后可以实现如下效果：
![[Pasted image 20211205170211.png]]
之后需要大家自行读取ScriptableObject中的内容，接入到实际的行为树中。
