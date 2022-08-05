using Jotunn.Utils;
using System.Collections.Generic;

namespace PlanBuild.Blueprints
{
    internal class UndoRemove : UndoActions.UndoRemove
    {
        public UndoRemove(IEnumerable<ZDO> data) : base(data)
        {
        }

        public override void Redo()
        {
            foreach (var zdo in Data)
            {
                Selection.Instance.Remove(zdo.m_uid);
            }
            base.Redo();
        }
    }
}