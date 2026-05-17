namespace CodeWF.MindView;

public interface IMindMapEditorController
{
    int GetLevel(MindMapNode node);

    bool IsRoot(MindMapNode? node);

    MindMapNode HandleMapEnter(MindMapNode node);

    MindMapNode HandleMapTab(MindMapNode node);

    bool PromoteNode(MindMapNode? node);

    MindMapNode DeleteNode(MindMapNode? node);

    bool CanMoveNode(MindMapNode? node, MindMapNode? target);

    bool MoveNode(MindMapNode? node, MindMapNode? target, MindMapDropPlacement placement);
}
