using Jotunn.Utils;
using System.Collections.Generic;

namespace PlanBuild.Blueprints
{
    internal class UndoCreate : UndoActions.UndoCreate
    {
        public UndoCreate(IEnumerable<ZDO> data) : base(data)
        {
        }

        public override void Undo()
        {
            foreach (var zdo in Data)
            {
                Selection.Instance.Remove(zdo.m_uid);
            }
            base.Undo();
        }
    }
}
