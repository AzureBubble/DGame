namespace GameLogic
{
    public class SelectNode : BaseNode
    {
        [Input]public BaseNode PreNode;
        [Output]public BaseNode NextNode;
    }
}