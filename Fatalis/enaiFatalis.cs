using ImGuiNET;
using SharpPluginLoader.Core;
using SharpPluginLoader.Core.Actions;
using SharpPluginLoader.Core.Entities;
using System.Numerics;
using System.Threading;

namespace enaiFatalis
{
    public class enaiFatalis : IPlugin
    {
        public string Name => "enaiFatalis";
        public string Author => "Seka";

        private int _previousAction;
        private int _actionId;
        private Vector3 _originalPosition;
        private Vector3 _abovePlayer;
        private Vector3 _aboveSelf;
        private Monster? _fatalis;
        private int _firstMove = 7;
        private bool _modEnabled = false;
        private string _statusMessage = "";
        private int _frameCountdown = 0;
        private const int _framesForMessage = 60;
        private bool _monsterDied = false;
        private bool _chainOpening = false;
        private bool _chainFinalNovas = false;
        private bool _chainFirstNova = false;
        private bool _chainDeath = false;
        private bool _dyingSleep = false;

        public void ResetState()
        {
            _monsterDied = false; 
            _chainOpening = false; 
            _chainFinalNovas = false; 
            _chainFirstNova = false; 
            _chainDeath = false; 
            _dyingSleep = false; 
            _firstMove = 7;
            _frameCountdown = 0;
            _fatalis = null;
        }
        public void OnMonsterCreate(Monster monster)
        {
            if (monster.Type == MonsterType.Fatalis)
            {
                _fatalis = monster;
                _firstMove = 7; //without this, _firstMove stays at 0 after 1 quest.
                _monsterDied = false;
            }
        }
        public void OnPlayerAction(Player player, ref ActionInfo action)
        {
            if (player == null) return;
            if (!_modEnabled) return;
            if (Quest.CurrentQuestId == -1) return;
            if (_fatalis == null) return;
            _abovePlayer = player.Position;
            _abovePlayer.Y += 1000f;

            if (_firstMove > 0)
            {
                _firstMove--;

                switch (_firstMove)
                {
                    case 5:
                        _originalPosition = _fatalis.Position;
                        _fatalis.Resize(0.01f);
                        _fatalis.Teleport(_abovePlayer);
                        Monster.DisableSpeedReset();
                        _fatalis.Speed = 10f;
                        break;
                    case 3:
                        _fatalis.ForceAction(309);
                        break;
                    case 1:
                        _fatalis.Teleport(_originalPosition);
                        _fatalis.Resize(1f);
                        _fatalis.ForceAction(7);
                        break;
                }
            }
        }
        
        public unsafe void OnMonsterDeath(Monster monster) 
        {
            if (_fatalis == null) return;
            if (_fatalis.Health == 0f)
            {
                _monsterDied = true;
            }
        }

        
        public unsafe void OnImGuiRender()
        {
            var player = Player.MainPlayer;
            if (player == null) return;

            if (ImGui.Button("Toggle"))
            {
                if (Quest.CurrentQuestId == -1)
                {
                    _modEnabled = !_modEnabled;
                    _statusMessage = _modEnabled ? "Enabled." : "Disabled.";
                    ResetState();

                } else
                {
                    _statusMessage = "Cannot toggle while in quest.";
                }
                _frameCountdown = _framesForMessage; 
            }
            if (_frameCountdown > 0)
            {
                ImGui.Text(_statusMessage);
            }

            /*
            if (_fatalis == null)
                return;
            if (ImGui.Button("78hp"))
            {
                _fatalis.Health = _fatalis.MaxHealth * 0.77f;
            }
            if (ImGui.Button("50hp"))
            {
                _fatalis.Health = _fatalis.MaxHealth * 0.49f;
            }
            if (ImGui.Button("40hp"))
            {
                _fatalis.Health = _fatalis.MaxHealth * 0.39f;
            }
            if (ImGui.Button("25hp"))
            {
                _fatalis.Health = _fatalis.MaxHealth * 0.24f;
            }
            if (ImGui.Button("6hp"))
            {
                _fatalis.Health = _fatalis.MaxHealth * 0.05f;
            }*/
        }


        public unsafe void OnMonsterAction(Monster monster, ref int actionId)
        {
            if (!_modEnabled) return;
            if (monster.Type != MonsterType.Fatalis)
                return;

            if (_fatalis == null)
                return;
            var HpPercent = _fatalis.Health / _fatalis.MaxHealth * 100.0f;

            if (HpPercent == 100f)
            {
                if (actionId == 7)
                {
                    actionId = 24;
                    _chainOpening = true;
                }
                else if (_chainOpening)
                {
                    switch (_previousAction)
                    {
                        case 24:
                            actionId = 26;
                            break;
                        case 26:
                            _fatalis.Speed = 1f;
                            Monster.EnableSpeedReset();
                            actionId = 100;
                            break;
                        case 100:
                            actionId = 202;
                            break;
                        case 202:
                            actionId = 23;
                            break;
                        case 23:
                            actionId = 101;
                            break;
                        case 101:
                            actionId = 100;
                            _chainOpening = false;
                            break;
                    }
                }
            }

            // 1st Nova chain
            if (HpPercent >= 70f && HpPercent <= 99f) 
            {
                if (actionId == 165)
                {
                    actionId = 25;
                    _chainFirstNova = true;
                }
                else if (_chainFirstNova)
                {
                    switch (_previousAction)
                    {
                        case 25:
                            actionId = 102;
                            break;
                        case 102:
                            actionId = 103;
                            break;
                        case 103:
                            actionId = 104;
                            break;
                        case 104:
                            actionId = 167;
                            break;
                        case 167:
                            actionId = 179;
                            _chainFirstNova = false;
                            break;
                    }
                }
            }

            // final novas
            if (HpPercent <= 68f)
            {
                if (actionId == 191 && !_chainFinalNovas) // FLY_FINAL_MODE_STAGE_BREATH_1ST_START
                {
                    actionId = 173; //1ST_START_02
                    _chainFinalNovas = true;
                }
                else if (_chainFinalNovas)
                {
                    switch (_previousAction)
                    {
                        case 173:
                            actionId = 174; //1ST_LOOP_02
                            break;
                        case 174:
                            actionId = 103; //FLY_FINAL_MODE_STAGE_BREATH_1ST_START
                            break;
                        case 103:
                            actionId = 192; //FLY_FINAL_MODE_STAGE_BREATH_1ST_LOOP
                            break;
                        case 192: 
                            actionId = 193;
                            break;
                        case 193: //FLY_FINAL_MODE_STAGE_BREATH_2ND_START
                            actionId = 196;
                            break; 
                        case 196: //FLY_FINAL_MODE_STAGE_BREATH_RETURN_START_LOOP
                            actionId = 197;
                            _chainFinalNovas = false;
                            break;
                    }
                }
            }

            _aboveSelf = _fatalis.Position;
            _aboveSelf.Y += 10f;

            // death nova
            if (_monsterDied)
            {
                if (actionId >= 278 && actionId <= 281)
                {
                    Monster.DisableSpeedReset();
                    _fatalis.Speed = 2f;
                    _fatalis.Teleport(_aboveSelf);
                    actionId = 186;
                    _chainDeath = true;
                }
                else if (_chainDeath)
                {
                    switch (_previousAction)
                    {
                        case 186:
                            actionId = 162;
                            break;
                        case 162:
                            _fatalis.Speed = 1f;
                            actionId = 170;
                            break;
                        case 170:
                            actionId = 171;
                            break;
                        case 171:
                            _fatalis.Speed = 3f;
                            actionId = 172;
                            break;
                        case 172:
                            actionId = 173;
                            break;
                        case 173:
                            actionId = 178;
                            break;
                        case 178:
                            actionId = 279;
                            Monster.EnableSpeedReset();
                            break;
                        case 279:
                            actionId = 258;
                            _dyingSleep = true;
                            _chainDeath = false;
                            _fatalis = null;
                            break;
                    }
                }
            }
            _previousAction = actionId;
        }
    }
}