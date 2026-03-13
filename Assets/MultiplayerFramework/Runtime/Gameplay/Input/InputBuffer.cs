using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MultiplayerFramework.Runtime.Gameplay.Input
{
    public sealed class InputBuffer
    {
        private readonly Dictionary<int, PlayerInputCommand> _commands = new();
        public void Store(PlayerInputCommand command)
        {
            _commands[command.Tick] = command;
        }
        
        public bool TryGet(int tick, out PlayerInputCommand command)
        {
            return _commands.TryGetValue(tick, out command);
        }

        public PlayerInputCommand GetOrDefault(int tick)
        {
            return _commands.TryGetValue(tick, out var command)
                ? command
                : PlayerInputCommand.Default(tick);
        }

        public void ClearBefore(int minTick)
        {
            if (_commands.Count == 0)
                return;

            List<int> removeKeys = null;

            foreach (var pair in _commands)
            {
                if (pair.Key < minTick)
                {
                    removeKeys ??= new List<int>();
                    removeKeys.Add(pair.Key);
                }
            }

            if (removeKeys == null)
                return;

            for (int i = 0; i < removeKeys.Count; i++)
            {
                _commands.Remove(removeKeys[i]);
            }
        }
    }
}

