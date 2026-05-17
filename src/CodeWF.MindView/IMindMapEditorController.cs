namespace CodeWF.MindView;

public interface IMindMapEditorController
{
    int GetLevel(MindMapNode node);

    bool IsRoot(MindMapNode? node);

    MindMapNode HandleMapEnter(MindMapNode node);

    MindMapNode HandleMapTab(MindMapNode node);

    MindMapNode AddChild(MindMapNode? parent, string title = "新主题");

    MindMapNode AddSibling(MindMapNode? node, string title = "新主题");

    bool CanPromoteNode(MindMapNode? node);

    bool PromoteNode(MindMapNode? node);

    bool CanDemoteNode(MindMapNode? node);

    bool DemoteNode(MindMapNode? node);

    bool CanMoveNodeUp(MindMapNode? node) => false;

    bool MoveNodeUp(MindMapNode? node) => false;

    bool CanMoveNodeDown(MindMapNode? node) => false;

    bool MoveNodeDown(MindMapNode? node) => false;

    MindMapNode DeleteNode(MindMapNode? node);

    bool CanMoveNode(MindMapNode? node, MindMapNode? target);

    bool MoveNode(MindMapNode? node, MindMapNode? target, MindMapDropPlacement placement);
}
