using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACE.Managers;

namespace ACE.Network.GameAction.Actions
{
    public class GameActionChangeFellowshipOpenness
    {
        [GameAction(GameActionType.ChangeFellowOpenness)]
        public static void Handle(ClientMessage message, Session session)
        {
            FellowshipManager.ChangeFellowshipOpenness(session.Player);
        }
    }
}
