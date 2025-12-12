using System;
using System.Collections.Generic;

namespace Ashore.AI.DecisionTree
{
    // Minimal decision tree framework for enemies/NPCs.
    public abstract class Node
    {
        public abstract Node Evaluate();
    }

    public class ActionNode : Node
    {
        private readonly Action _action;
        public ActionNode(Action action) { _action = action; }
        public override Node Evaluate() { _action?.Invoke(); return this; }
    }

    public class ConditionNode : Node
    {
        private readonly Func<bool> _predicate;
        private readonly Node _trueNode;
        private readonly Node _falseNode;
        public ConditionNode(Func<bool> predicate, Node trueNode, Node falseNode)
        {
            _predicate = predicate; _trueNode = trueNode; _falseNode = falseNode;
        }
        public override Node Evaluate() { return (_predicate?.Invoke() ?? false) ? _trueNode : _falseNode; }
    }

    public class DecisionTree
    {
        private readonly Node _root;
        public DecisionTree(Node root) { _root = root; }
        public void Tick() { _root?.Evaluate(); }
    }
}
