using Avalonia.Input;

namespace CodeWF.MindView;

public static class MindMapKeyboardGestureRouter
{
    public static MindMapKeyboardAction ResolveTitleAction(Key key, KeyModifiers modifiers, bool isTitleEmpty)
    {
        if (IsEnterKey(key))
        {
            return MindMapKeyboardAction.AddFromEnter;
        }

        if (key == Key.Tab)
        {
            return modifiers.HasFlag(KeyModifiers.Shift)
                ? MindMapKeyboardAction.Promote
                : MindMapKeyboardAction.Demote;
        }

        return IsDeleteKey(key) && isTitleEmpty
            ? MindMapKeyboardAction.DeleteEmptyTitle
            : MindMapKeyboardAction.None;
    }

    public static MindMapKeyboardAction ResolveFrameAction(Key key, KeyModifiers modifiers)
    {
        if (IsDeleteKey(key))
        {
            return MindMapKeyboardAction.DeleteSelected;
        }

        if (IsEnterKey(key))
        {
            return MindMapKeyboardAction.AddFromEnter;
        }

        if (key == Key.Tab)
        {
            return modifiers.HasFlag(KeyModifiers.Shift)
                ? MindMapKeyboardAction.Promote
                : MindMapKeyboardAction.Demote;
        }

        return MindMapKeyboardAction.None;
    }

    public static MindMapKeyboardAction ResolveNoteAction(Key key, bool isNoteEmpty)
    {
        return IsDeleteKey(key) && isNoteEmpty
            ? MindMapKeyboardAction.DeleteEmptyNote
            : MindMapKeyboardAction.None;
    }

    public static bool IsEnterKey(Key key)
    {
        return key is Key.Enter or Key.Return;
    }

    public static bool IsDeleteKey(Key key)
    {
        return key is Key.Delete or Key.Back;
    }
}
