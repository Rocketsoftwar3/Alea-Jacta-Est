using System.Numerics;
using ImGuiNET;
using Alea_Jacta_Est.Commands;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.ImGuiBackend;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

public class ImGuiOverlayService
{
    private bool _marketOpen;

    private readonly CardInteractionService _cardInteraction;
    private readonly MarketWindowService _marketWindow;
    private readonly HandWindowService _handWindow;
    private readonly BoardWindowService _boardWindow;
    private readonly TargetSelectionWindowService _targetSelection;
    private readonly VictoryScreenService _victoryScreen;
    private readonly CommandQueue _commands;

    public ImGuiOverlayService(
        CardInteractionService cardInteraction,
        MarketWindowService marketWindow,
        HandWindowService handWindow,
        BoardWindowService boardWindow,
        TargetSelectionWindowService targetSelection,
        VictoryScreenService victoryScreen,
        CommandQueue commands)
    {
        _cardInteraction = cardInteraction;
        _marketWindow = marketWindow;
        _handWindow = handWindow;
        _boardWindow = boardWindow;
        _targetSelection = targetSelection;
        _victoryScreen = victoryScreen;
        _commands = commands;
    }

    public void Render(GameState state, GraphicsResources gfx, ImGuiRenderer imGuiRenderer)
    {
        _cardInteraction.Render(state, gfx, imGuiRenderer);

        RenderGameStatePanel(state);
        RenderPlayersPanel(state);
        _handWindow.Render(state, imGuiRenderer);
        _boardWindow.Render(state, imGuiRenderer);
        _marketWindow.Render(state, imGuiRenderer, ref _marketOpen);
        _targetSelection.Render(state);
        _victoryScreen.Render(state);
    }

    private void RenderGameStatePanel(GameState state)
    {
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(240, 200), ImGuiCond.FirstUseEver);
        ImGui.Begin("Partie");

        var stateLabel = state.Phase switch
        {
            GamePhase.WaitingForPlayers => "En attente",
            GamePhase.InProgress        => "En cours",
            GamePhase.Finished          => "Terminée",
            _                           => "?"
        };
        ImGui.Text($"État      : {stateLabel}");
        ImGui.Text($"Tour      : {state.CurrentTurn}");

        // Phase indicator with color
        var (phaseLabel, phaseColor) = state.CurrentTurnPhase switch
        {
            TurnPhase.DrawPhase       => ("Pioche",     new Vector4(0.5f, 0.8f, 1f, 1f)),
            TurnPhase.PlayPhase       => ("Jeu",        new Vector4(0.3f, 1f, 0.3f, 1f)),
            TurnPhase.ValidatePhase   => ("Validation", new Vector4(1f, 0.9f, 0.2f, 1f)),
            TurnPhase.ResolutionPhase => ("Résolution", new Vector4(1f, 0.4f, 0.4f, 1f)),
            TurnPhase.CleanupPhase    => ("Nettoyage",  new Vector4(0.8f, 0.5f, 1f, 1f)),
            TurnPhase.ShopPhase       => ("Boutique",   new Vector4(1f, 0.7f, 0.3f, 1f)),
            _                         => ("?",          new Vector4(1f, 1f, 1f, 1f))
        };
        ImGui.Text("Phase     : ");
        ImGui.SameLine();
        ImGui.TextColored(phaseColor, phaseLabel);

        ImGui.Separator();

        // Validate turn button (PlayPhase only)
        bool isPlayPhase = state.CurrentTurnPhase == TurnPhase.PlayPhase;
        int localIndex = state.Players.IndexOf(state.LocalPlayer);
        bool alreadyValidated = state.TurnStates.TryGetValue(localIndex, out var ts) && ts.HasValidated;

        if (!isPlayPhase || alreadyValidated) ImGui.BeginDisabled();
        if (ImGui.Button("Valider le tour") && isPlayPhase && !alreadyValidated)
            _commands.Enqueue(new ValidateTurnCommand(localIndex));
        if (!isPlayPhase || alreadyValidated) ImGui.EndDisabled();

        // End shop button (ShopPhase only)
        bool isShopPhase = state.CurrentTurnPhase == TurnPhase.ShopPhase;
        if (!isShopPhase) ImGui.BeginDisabled();
        if (ImGui.Button("Fin de boutique"))
            _commands.Enqueue(new EndShopPhaseCommand());
        if (!isShopPhase) ImGui.EndDisabled();

        ImGui.Separator();
        string marketLabel = _marketOpen ? "Fermer Marché" : "Ouvrir Marché";
        if (ImGui.Button(marketLabel))
            _marketOpen = !_marketOpen;

        ImGui.End();
    }

    private void RenderPlayersPanel(GameState state)
    {
        ImGui.SetNextWindowPos(new Vector2(10, 220), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(280, 300), ImGuiCond.FirstUseEver);
        ImGui.Begin("Joueurs");

        foreach (var player in state.Players)
        {
            bool isLocal = player.IsLocalPlayer;
            string label = isLocal ? $"{player.Name} (local)" : player.Name;

            if (ImGui.CollapsingHeader(label, isLocal ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None))
            {
                ImGui.Indent();

                ImGui.Text($"PV        : {player.Health}");
                ImGui.Text($"Pièces    : {player.Wallet}");
                ImGui.Text($"Remise    : {player.Market.Discount * 100:F0}%%");
                ImGui.Separator();

                // Show relevant decks only
                var decksToShow = isLocal
                    ? new[] { "MainDeck", "HandDeck", "BoardDeck0", "DiscardDeck", "SpecialDeck", "ArcanaHandDeck", "ArcanaDiscardDeck" }
                    : new[] { "MainDeck", "SpecialDeck" };

                foreach (var deckName in decksToShow)
                {
                    if (player.Decks.TryGetValue(deckName, out var deck))
                        ImGui.Text($"{deckName,-20}: {deck.Cards.Count}");
                }

                // Impératrice endroit : afficher la main des adversaires si le flag est actif
                if (!isLocal)
                {
                    bool canSeeHands = state.TurnStates.TryGetValue(0, out var localTs)
                        && localTs.CanSeeOpponentHands;
                    if (canSeeHands && player.Decks.TryGetValue("HandDeck", out var opponentHand)
                        && opponentHand.Cards.Count > 0)
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 1f, 0.8f, 1f), "Main visible (Impératrice) :");
                        foreach (var c in opponentHand.Cards)
                        {
                            string cardLabel = c is Entities.ValueCard ovc ? ovc.DisplayName
                                : c is Entities.ArcanaCard oac ? oac.ArcanaName
                                : c.TextureRecto.Name;
                            ImGui.TextDisabled($"  {cardLabel}");
                        }
                    }
                }

                ImGui.Unindent();
            }
        }

        ImGui.End();
    }
}
