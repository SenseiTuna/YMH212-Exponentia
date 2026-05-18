using System;
using UnityEngine;

namespace Exponentia.InventorySystem
{
    [Serializable]
    public class RoomRewardContext
    {
        public RewardContextType contextType = RewardContextType.RoomClear;
        public int floorIndex = 1;
        public bool allowVoidRewards = true;
        public string customTag;

        public static RoomRewardContext DefaultRoomClear()
        {
            return new RoomRewardContext
            {
                contextType = RewardContextType.RoomClear,
                floorIndex = 1,
                allowVoidRewards = true
            };
        }
    }
}
