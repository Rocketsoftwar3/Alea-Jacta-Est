using System.Numerics;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.ImGuiBackend;
using Alea_Jacta_Est.Main;
using ImGuiNET;

namespace Alea_Jacta_Est.Services;

public class ImGuiOverlayService
{
    private bool _marketOpen;

    private readonly CardInteractionService _cardInteraction;
    private readonly MarketWindowService _marketWindow;

    public ImGuiOverlayService(CardInteractionService cardInteraction, MarketWindowService marketWindow)
    {
        _cardInteraction = cardInteraction;
        _marketWindow = marketWindow;
    }

    public void Render(GameState state, GraphicsResources gfx, ImGuiRenderer imGuiRenderer)
    {
        _cardInteraction.Render(state, gfx, imGuiRenderer);

        RenderGameStatePanel(state);
        RenderPlayersPanel(state);
        _marketWindow.Render(state, imGuiRenderer, ref _marketOpen);
    }

    private void RenderGameStatePanel(GameState state)
    {
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(220, 140), ImGuiCond.FirstUseEver);
        ImGui.Begin("Partie");

        var stateLabel = state.Phase switch
        {
            GamePhase.WaitingForPlayers => "En attente",
            GamePhase.InProgress        => "En cours",
            GamePhase.Finished          => "Terminee",
            _                           => "?"
        };
        ImGui.Text($"Etat      : {stateLabel}");
        ImGui.Text($"Tour      : {state.CurrentTurn}");
        ImGui.Text($"Joueurs   : {state.Players.Count} / {GameState.MaxPlayers}");

        var current = state.CurrentPlayer;
        ImGui.Text($"Au tour de: {(current != null ? current.Name : "-")}");

        ImGui.Separator();
        string marketLabel = _marketOpen ? "Fermer Marché" : "Ouvrir Marché";
        if (ImGui.Button(marketLabel))
            _marketOpen = !_marketOpen;

        ImGui.End();
    }

    private void RenderPlayersPanel(GameState state)
    {
        ImGui.SetNextWindowPos(new Vector2(10, 160), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(280, 300), ImGuiCond.FirstUseEver);
        ImGui.Begin("Joueurs");

        foreach (var player in state.Players)
        {
            bool isLocal = player.IsLocalPlayer;
            string label = isLocal ? $"{player.Name} (local)" : player.Name;

            if (ImGui.CollapsingHeader(label, isLocal ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None))
            {
                ImGui.Indent();

                ImGui.Text($"Pieces    : {player.Wallet}");
                ImGui.Text($"Remise    : {player.Market.Discount * 100:F0}%%");
                ImGui.Separator();

                foreach (var (deckName, deck) in player.Decks)
                {
                    ImGui.Text($"{deckName,-14}: {deck.Cards.Count} carte(s)");
                }

                ImGui.Unindent();
            }
        }

        ImGui.End();
    }
}
