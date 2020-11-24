using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using JinrouClient.Cosole;
using JinrouClient.Data;
using JinrouClient.Data.Repository;
using JinrouClient.Domain;
using JinrouClient.Usecase;
using Reactive.Bindings;

var address = "http://localhost:50051";
if (args.Length > 0)
{
    address = args[0];
}

var secureStorage = new DummySecureStorage();
var clientProvider = new DotNetJinrouClientProvider(address);
var authRepository = new AuthRepository(clientProvider);
var gameRepository = new GameRepository(clientProvider);
var userRepository = new UserRepository(secureStorage);
var userUsecase = new UserUsecase(userRepository, authRepository);
var gameUsecase = new GameUsecase(gameRepository, userRepository, authRepository);

Console.WriteLine("名前を入力して下さい");
var name = Console.ReadLine();

userUsecase.Register(name!);
var user = await userUsecase.UserRegistered.FirstAsync();

var phaseChased = gameUsecase.PhaseChanged
    .Publish<GameInfo?>(null);

var errorOccurred = gameUsecase.ErrorOccurred;

for (; ; )
{
    using IDisposable subscription = phaseChased.Connect();

    GameInfo gameInfo;

    gameInfo = await SelectMode();

    gameInfo = await FirstNightPhase(gameInfo);

    for (; ; )
    {
        gameInfo = await NoonPhase(gameInfo);

        if (gameInfo.Phase == Phase.End)
        {
            break;
        }

        gameInfo = await NightPhase(gameInfo);

        if (gameInfo.Phase == Phase.End)
        {
            break;
        }
    }

    EndPhase(gameInfo);

    Console.WriteLine("何かキーを押して下さい");
    Console.ReadKey();
}

async Task<GameInfo> SelectMode()
{
    Console.WriteLine("開始方法を選択して下さい");
    Console.WriteLine("1: 新規で始める");
    Console.WriteLine("2: 参加する");

    for (; ; )
    {
        var mode = Console.ReadLine();

        switch (mode)
        {
            case "1":
                await StartNewGame();
                break;
            case "2":
                await Join();
                break;
            default:
                Console.WriteLine("正しく入力して下さい");
                continue;
        }

        Console.WriteLine("他のプレイヤーを待っています");

        return await phaseChased.Where(x => x is not null && x.Phase == Phase.Night)
            .FirstAsync();
    }
}

async Task StartNewGame()
{
    for (; ; )
    {
        Console.WriteLine("参加人数を入力して下さい");
        var playerNumStr = Console.ReadLine();

        Console.WriteLine("人狼の数を入力して下さい");
        var wearwolfNumStr = Console.ReadLine();

        if (int.TryParse(playerNumStr, out var playerNum) &&
            int.TryParse(wearwolfNumStr, out var wearwolfNum))
        {
            var config = new GameConfig
            {
                PlayerNum = playerNum,
                WerewolfNum = wearwolfNum
            };

            var started = Observable.Amb(
                gameUsecase.StartNewGameResponsed.Where(x => x is not null)
                    .Select<Game?, (bool isCompleted, Game? game, Exception? error)>(x => (true, x, null)),
                errorOccurred.Where(x => x is not null)
                    .Select<Exception, (bool isCompleted, Game? game, Exception? error)>(x => (true, null, x)))
                .Publish((false, null, null));

            using (started.Connect())
            {
                gameUsecase.StartNewGame(config);

                var (_, game, error) = await started.Where(x => x.isCompleted).FirstAsync();

                if (error is not null)
                {
                    Console.WriteLine("エラーが発生しました");
                    await Task.Delay(1);
                    continue;
                }

                Console.WriteLine($"ゲームID: {game.GameId}");
            }

            break;
        }
        else
        {
            Console.WriteLine("正しく入力して下さい");
        }
    }
}

async Task Join()
{
    var published = gameUsecase.PhaseChanged.Publish(GameInfo.Default);

    for (; ; )
    {
        Console.WriteLine("ゲームIDを入力して下さい");
        var gameId = Console.ReadLine();

        var joined = Observable.Amb(
            gameUsecase.JoinResponsed.Where(x => x is not null)
                .Select<Game?, (bool isCompleted, Game? game, Exception? error)>(x => (true, x, null)),
            errorOccurred.Where(x => x is not null)
                .Select<Exception, (bool isCompleted, Game? game, Exception? error)>(x => (true, null, x)))
            .Publish((false, null, null));

        using (joined.Connect())
        {
            gameUsecase.Join(gameId!);

            var (_, game, error) = await joined.Where(x => x.isCompleted).FirstAsync();

            if (error is not null)
            {
                Console.WriteLine("エラーが発生しました");
                await Task.Delay(1);
                continue;
            }
        }

        break;
    }
}

async Task<GameInfo> FirstNightPhase(GameInfo gameInfo)
{
    if (gameUsecase.MyPlayer.Value is null)
        throw new InvalidOperationException();

    var myPlayer = gameUsecase.MyPlayer.Value;

    Console.WriteLine("第1の夜");
    Console.WriteLine($"あたたは、{myPlayer.Index}番 {GetRoleName(myPlayer.Role)}です");

    DisplayPlayers();

    Console.WriteLine("何かキーを押して下さい");
    Console.ReadKey();

    gameUsecase.Next(gameInfo);

    Console.WriteLine("他のプレイヤーを待っています");

    return await phaseChased.Where(info => info is not null && info.Phase == Phase.Noon)
        .FirstAsync();
}

async Task<GameInfo> NoonPhase(GameInfo gameInfo)
{
    if (gameUsecase.MyPlayer.Value is null)
        throw new InvalidOperationException();

    Console.WriteLine($"第{gameInfo.Day}の昼");

    if (gameInfo.KilledPlayer is null)
    {
        Console.WriteLine("誰も殺されませんでした");
    }
    else
    {
        Console.WriteLine($"{gameInfo.KilledPlayer.Index}番の{gameInfo.KilledPlayer.Name}さんが殺されました");
    }

    DisplayPlayers();

    if (gameUsecase.MyPlayer.Value.IsDied)
    {
        Console.WriteLine("何かキーを押して下さい");
        Console.ReadKey();

        gameUsecase.Next(gameInfo);

        Console.WriteLine("他のプレイヤーを待っています");

        return await phaseChased.Where(info => info is not null && info.Phase is Phase.Night or Phase.End)
            .FirstAsync();
    }
    else
    {
        for (; ; )
        {
            Console.WriteLine("処刑するプレイヤー番号を投票して下さい (0は棄権)");

            var player = SelectPlayer();

            var voted = Observable.Amb(
                gameUsecase.VoteResponsed.Select<Unit, (bool isCompleted, Exception? error)>(x => (true, null)),
                errorOccurred.Where(x => x is not null).Select<Exception, (bool isCompleted, Exception? error)>(x => (true, x)))
                .Publish((false, null));

            using (voted.Connect())
            {
                gameUsecase.Vote(gameInfo, player);

                var (_, error) = await voted.Where(x => x.isCompleted).FirstAsync();

                if (error is not null)
                {
                    Console.WriteLine("エラーが発生しました");
                    await Task.Delay(1);
                    continue;
                }
            }

            Console.WriteLine("他のプレイヤーを待っています");

            return await phaseChased
                .Where(info => info is not null && info.Phase is Phase.Night or Phase.End)
                .FirstAsync();
        }
    }
}

async Task<GameInfo> NightPhase(GameInfo gameInfo)
{
    if (gameUsecase.MyPlayer.Value is null)
        throw new InvalidOperationException();

    Console.WriteLine($"第{gameInfo.Day}の夜");

    if (gameInfo.KilledPlayer is null)
    {
        Console.WriteLine("誰も処刑されませんでした");
    }
    else
    {
        Console.WriteLine($"{gameInfo.KilledPlayer.Index}番の{gameInfo.KilledPlayer.Name}さんが処刑されました");
    }

    DisplayPlayers();

    if (gameUsecase.MyPlayer.Value.Role == Role.Werewolf)
    {
        for (; ; )
        {
            Console.WriteLine("殺すプレイヤー番号を選んで下さい");

            var player = SelectPlayer();

            var killed = Observable.Amb(
                gameUsecase.KillResponsed.Select<Unit, (bool isCompleted, Exception? error)>(x => (true, null)),
                errorOccurred.Where(x => x is not null).Select<Exception, (bool isCompleted, Exception? error)>(x => (true, x)))
                .Publish((false, null));

            using (killed.Connect())
            {
                gameUsecase.Kill(gameInfo, player);

                var (_, error) = await killed.Where(x => x.isCompleted)
                    .FirstAsync();

                if (error is not null)
                {
                    Console.WriteLine("エラーが発生しました");
                    await Task.Delay(1);
                    continue;
                }
            }

            Console.WriteLine("他のプレイヤーを待っています");

            return await phaseChased.Where(info => info is not null && info.Phase is Phase.Noon or Phase.End)
                    .FirstAsync();
        }
    }
    else
    {
        Console.WriteLine("何かキーを押して下さい");
        Console.ReadKey();

        gameUsecase.Next(gameInfo);

        Console.WriteLine("他のプレイヤーを待っています");

        return await phaseChased.Where(info => info is not null && info.Phase is Phase.Noon or Phase.End)
                .FirstAsync();
    }
}

void EndPhase(GameInfo gameInfo)
{
    if (gameInfo.Winner is not null)
    {
        var mySide = gameUsecase.MyPlayer.Value!.Side;

        if (gameInfo.Winner == mySide)
        {
            Console.WriteLine($"{GetSideName(mySide)}の勝利です");
        }
        else
        {
            Console.WriteLine($"{GetSideName(mySide)}の敗北です");
        }
    }
}

void DisplayPlayers()
{
    foreach (var player in gameUsecase.Players)
    {
        var sb = new StringBuilder();

        sb.Append($"{player.Index}: {player.Name}");

        if (player.Role != Role.Unkown)
        {
            sb.Append($" {GetRoleName(player.Role)}");
        }
        else if (player.Side != Side.Neutral)
        {
            sb.Append($" {GetSideName(player.Side)}");
        }

        if (player.IsDied)
        {
            sb.Append($" 死亡");
        }

        Console.WriteLine(sb.ToString());
    }
}

Player? SelectPlayer()
{
    for (; ; )
    {
        var indexStr = Console.ReadLine();

        if (int.TryParse(indexStr, out var index))
        {
            if (index >= 1 && index <= gameUsecase.Players.Count)
            {
                return gameUsecase.Players[index - 1];
            }
            else if (index == 0)
            {
                return null;
            }
            else
            {
                Console.WriteLine("正しく入力して下さい");
            }
        }
        else
        {
            Console.WriteLine("正しく入力して下さい");
        }
    }
}

string GetRoleName(Role role) => role switch
{
    Role.Unkown => "",
    Role.Villager => "村人",
    Role.Werewolf => "人狼",
    _ => throw new ArgumentOutOfRangeException(nameof(role)),
};

string GetSideName(Side side) => side switch
{
    Side.Neutral => "",
    Side.Villagers => "村人陣営",
    Side.Werewolves => "人狼陣営",
    _ => throw new ArgumentOutOfRangeException(nameof(side)),
};