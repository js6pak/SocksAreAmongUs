using System.Reflection;
using System.Threading.Tasks;
using Impostor.Api.Games;
using Impostor.Api.Net;
using Impostor.Api.Net.Inner.Objects;
using Impostor.Api.Net.Messages;
using Impostor.Api.Plugins;
using Reactor.Impostor.Rpcs;

namespace SocksAreAmongUs.Impostor
{
    [ImpostorPlugin(Id, "SocksAreAmongUs", "js6pak", "0.1.0")]
    public class SocksAreAmongUsPlugin : PluginBase
    {
        public const string Id = "pl.js6pak.SocksAreAmongUs";

        private readonly IReactorCustomRpcManager _reactorCustomRpcManager;

        public SocksAreAmongUsPlugin(IReactorCustomRpcManager reactorCustomRpcManager)
        {
            _reactorCustomRpcManager = reactorCustomRpcManager;
        }

        public override ValueTask EnableAsync()
        {
            _reactorCustomRpcManager.Register<RpcSetDead>();
            _reactorCustomRpcManager.Register<RpcSetImpostor>();

            return default;
        }
    }

    public class RpcSetDead : ReactorCustomRpc<IInnerPlayerControl>
    {
        public override string ModId => SocksAreAmongUsPlugin.Id;

        public override uint Id => 1;

        public override ValueTask<bool> HandleAsync(IInnerPlayerControl player, IClientPlayer sender, IClientPlayer? target, IMessageReader reader)
        {
            Deserialize(reader, out var isDead);

            player.PlayerInfo.GetType()
                    .GetProperty(nameof(IInnerPlayerInfo.IsDead), BindingFlags.Instance | BindingFlags.Public)!
                .SetValue(player.PlayerInfo, isDead);

            return new ValueTask<bool>(true);
        }

        public static void Serialize(IMessageWriter writer, bool isDead)
        {
            writer.Write(isDead);
        }

        public static void Deserialize(IMessageReader reader, out bool isDead)
        {
            isDead = reader.ReadBoolean();
        }
    }

    public class RpcSetImpostor : ReactorCustomRpc<IInnerPlayerControl>
    {
        public override string ModId => SocksAreAmongUsPlugin.Id;

        public override uint Id => 2;

        public override ValueTask<bool> HandleAsync(IInnerPlayerControl player, IClientPlayer sender, IClientPlayer? target, IMessageReader reader)
        {
            Deserialize(reader, player.Game, out var targetPlayer, out var isImpostor);

            if (targetPlayer == null)
            {
                return new ValueTask<bool>(false);
            }

            targetPlayer.PlayerInfo.GetType()
                    .GetProperty(nameof(IInnerPlayerInfo.IsImpostor), BindingFlags.Instance | BindingFlags.Public)!
                .SetValue(targetPlayer.PlayerInfo, isImpostor);

            return new ValueTask<bool>(true);
        }

        public static void Serialize(IMessageWriter writer, IInnerPlayerControl player, bool isImpostor)
        {
            writer.Write(player);
            writer.Write(isImpostor);
        }

        public static void Deserialize(IMessageReader reader, IGame game, out IInnerPlayerControl? player, out bool isImpostor)
        {
            player = reader.ReadNetObject<IInnerPlayerControl>(game);
            isImpostor = reader.ReadBoolean();
        }
    }
}
