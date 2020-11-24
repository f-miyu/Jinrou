package infrastracture

import (
	"context"
	"errors"

	"github.com/f-miyu/jinrou/server/app/domain"
	"github.com/f-miyu/jinrou/server/app/pb"
	"github.com/f-miyu/jinrou/server/app/usecase"
	grpc_auth "github.com/grpc-ecosystem/go-grpc-middleware/auth"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"
)

type tokenKey struct{}

type JinrouServer struct {
	pb.UnimplementedJinrouServer
	gameUsecase usecase.GameUsecase
	authUsecase usecase.AuthUsecase
}

func NewJinrouServer(gameUsecase usecase.GameUsecase, authUsecase usecase.AuthUsecase) *JinrouServer {
	return &JinrouServer{
		gameUsecase: gameUsecase,
		authUsecase: authUsecase,
	}
}

func (s *JinrouServer) Register(ctx context.Context, in *pb.RegisterRequest) (res *pb.RegisterResponse, err error) {
	user, token, refreshToken, err := s.authUsecase.Register(in.PlayerName)
	if err != nil {
		return
	}
	res = &pb.RegisterResponse{
		PlayerId:     uint64(user.ID),
		Token:        token.String,
		RefreshToken: refreshToken.String,
	}

	return
}

func (s *JinrouServer) Refresh(ctx context.Context, in *pb.RefreshRequest) (res *pb.RefreshResponse, err error) {
	toekn, refreshToken, err := s.authUsecase.RefreshTokens(in.RefreshToken)
	if err != nil {
		return
	}
	res = &pb.RefreshResponse{
		Token:        toekn.String,
		RefreshToken: refreshToken.String,
	}
	return
}

func (s *JinrouServer) CreateGame(ctx context.Context, in *pb.CreateGameRequest) (res *pb.CreateGameResponse, err error) {
	userID, err := s.getUserID(ctx)
	if err != nil {
		return
	}

	config := domain.Config{
		PlayerNum:   int(in.Config.PlayerNum),
		WerewolfNum: int(in.Config.WerewolfNum),
	}
	state, err := s.gameUsecase.CreateGame(userID, config)
	if err != nil {
		return
	}

	res = &pb.CreateGameResponse{
		State: s.cnvertState(state),
	}

	return
}

func (s *JinrouServer) Join(ctx context.Context, in *pb.JoinRequest) (res *pb.JoinResponse, err error) {
	userID, err := s.getUserID(ctx)
	if err != nil {
		return
	}

	state, err := s.gameUsecase.Join(in.GameId, userID)
	if err != nil {
		return
	}

	res = &pb.JoinResponse{
		State: s.cnvertState(state),
	}

	return
}

func (s *JinrouServer) Leave(ctx context.Context, in *pb.LeaveRequest) (res *pb.LeaveResponse, err error) {
	userID, err := s.getUserID(ctx)
	if err != nil {
		return
	}

	state, err := s.gameUsecase.Leave(in.GameId, userID)
	if err != nil {
		return
	}

	res = &pb.LeaveResponse{
		State: s.cnvertState(state),
	}

	return
}

func (s *JinrouServer) Vote(ctx context.Context, in *pb.VoteRequest) (res *pb.VoteResponse, err error) {
	userID, err := s.getUserID(ctx)
	if err != nil {
		return
	}

	err = s.gameUsecase.Vote(in.GameId, userID, uint(in.PlayerId))
	if err != nil {
		return
	}

	res = &pb.VoteResponse{}

	return
}

func (s *JinrouServer) Kill(ctx context.Context, in *pb.KillRequest) (res *pb.KillResponse, err error) {
	userID, err := s.getUserID(ctx)
	if err != nil {
		return
	}

	err = s.gameUsecase.Kill(in.GameId, userID, uint(in.PlayerId))
	if err != nil {
		return
	}

	res = &pb.KillResponse{}

	return
}

func (s *JinrouServer) Next(ctx context.Context, in *pb.NextRequest) (res *pb.NextResponse, err error) {
	userID, err := s.getUserID(ctx)
	if err != nil {
		return
	}

	err = s.gameUsecase.Next(in.GameId, userID)
	if err != nil {
		return
	}

	res = &pb.NextResponse{}

	return
}

func (s *JinrouServer) GetRoles(ctx context.Context, in *pb.GetRolesRequest) (res *pb.GetRolesResponse, err error) {
	userID, err := s.getUserID(ctx)
	if err != nil {
		return
	}

	roles, err := s.gameUsecase.GetRoles(in.GameId, userID)
	if err != nil {
		return
	}

	res = &pb.GetRolesResponse{
		Roles: make(map[uint64]pb.Role),
	}

	for k, r := range roles {
		res.Roles[uint64(k)] = pb.Role(r)
	}

	return
}

func (s *JinrouServer) ObserveState(req *pb.ObserveStateRequest, stream pb.Jinrou_ObserveStateServer) (err error) {
	userID, err := s.getUserID(stream.Context())
	if err != nil {
		return
	}

	ch, err := s.gameUsecase.ObserveState(req.GameId, userID)
	if err != nil {
		return
	}

	for change := range ch {
		res := &pb.ObserveStateResponse{
			State:      s.cnvertState(change.State),
			ChangeType: pb.ChangeType(change.ChangeType),
			OldPhase:   pb.Phase(change.OldPhase),
		}
		switch change.ChangeType {
		case domain.PlayerJoined:
			res.Parameter = &pb.ObserveStateResponse_AddedPlayerId{
				AddedPlayerId: uint64(change.AddedPlayerID),
			}
		case domain.PlayerLeft:
			res.Parameter = &pb.ObserveStateResponse_LeftPlayerId{
				LeftPlayerId: uint64(change.LeftPlayerID),
			}
		case domain.PhaseChanged:
			res.Parameter = &pb.ObserveStateResponse_KilledPlayerId{
				KilledPlayerId: uint64(change.KilledPlayerID),
			}
		case domain.GameOver:
			res.Parameter = &pb.ObserveStateResponse_Winner{
				Winner: pb.Side(change.Winner),
			}
		}

		err = stream.Send(res)
		if err != nil {
			s.gameUsecase.UnobserveState(req.GameId, userID)
			return
		}
	}

	return
}

func (s *JinrouServer) UnobserveState(ctx context.Context, in *pb.UnobserveStateRequest) (res *pb.UnobserveStateResponse, err error) {
	userID, err := s.getUserID(ctx)
	if err != nil {
		return
	}

	err = s.gameUsecase.UnobserveState(in.GameId, userID)
	if err != nil {
		return
	}

	return
}

func (s *JinrouServer) AuthFuncOverride(ctx context.Context, fullMethodName string) (newCtx context.Context, err error) {
	if fullMethodName == "/jinrou.Jinrou/Register" || fullMethodName == "/jinrou.Jinrou/Refresh" {
		newCtx = ctx
		return
	}
	return s.Authenticate(ctx)
}

func (s *JinrouServer) Authenticate(ctx context.Context) (newCtx context.Context, err error) {
	token, err := grpc_auth.AuthFromMD(ctx, "bearer")
	if err != nil {
		st := status.New(codes.Unauthenticated, err.Error())
		err = st.Err()
		return
	}

	tokenData, err := s.authUsecase.VerifyToken(token)
	if err != nil {
		st := status.New(codes.Unauthenticated, err.Error())
		err = st.Err()
		return
	}

	newCtx = context.WithValue(ctx, tokenKey{}, tokenData)

	return
}

func (s *JinrouServer) getUserID(ctx context.Context) (userID uint, err error) {
	token, ok := ctx.Value(tokenKey{}).(domain.Token)
	if !ok {
		err = errors.New("no token")
		return
	}
	userID = token.UserID
	return
}

func (s *JinrouServer) cnvertState(state domain.State) *pb.State {
	players := make(map[uint64]*pb.Player)
	for k, p := range state.Players {
		player := &pb.Player{
			PlayerId:   uint64(p.ID),
			PlayerName: p.Name,
			IsDied:     p.IsDied,
			Index:      int32(p.Index),
		}
		players[uint64(k)] = player
	}

	return &pb.State{
		GameId: state.ID,
		Config: &pb.Config{
			PlayerNum:   int32(state.Config.PlayerNum),
			WerewolfNum: int32(state.Config.WerewolfNum),
		},
		Phase:   pb.Phase(state.Phase),
		Day:     int32(state.Day),
		Players: players,
	}
}
