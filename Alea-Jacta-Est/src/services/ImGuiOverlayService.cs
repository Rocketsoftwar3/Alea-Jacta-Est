using System.Numerics;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;
using ImGuiNET;

namespace Alea_Jacta_Est.Services;

/// <summary>Renders all game UI using Dear ImGui.</summary>
public static class ImGuiOverlayService
{
    public static void Render(GameContext ctx)
    {
        // Card layer first → rendered behind info panels (ImGui Z-order = creation order)
        CardInteractionService.Render(ctx);

        RenderGameStatePanel(ctx);
        RenderPlayersPanel(ctx);
    }

    private static void RenderGameStatePanel(GameContext ctx)
    {
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(220, 110), ImGuiCond.FirstUseEver);
        ImGui.Begin("Partie");

        var stateLabel = ctx.State switch
        {
            GameState.WaitingForPlayers => "En attente",
            GameState.InProgress        => "En cours",
            GameState.Finished          => "Terminee",
            _                           => "?"
        };
        ImGui.Text($"Etat      : {stateLabel}");
        ImGui.Text($"Tour      : {ctx.CurrentTurn}");
        ImGui.Text($"Joueurs   : {ctx.Players.Count} / {GameContext.MaxPlayers}");

        var current = ctx.CurrentPlayer;
        ImGui.Text($"Au tour de: {(current != null ? current.Name : "-")}");

        ImGui.End();
    }

    private static void RenderPlayersPanel(GameContext ctx)
    {
        ImGui.SetNextWindowPos(new Vector2(10, 130), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(280, 300), ImGuiCond.FirstUseEver);
        ImGui.Begin("Joueurs");

        foreach (var player in ctx.Players)
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
