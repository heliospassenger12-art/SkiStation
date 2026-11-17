using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    // Optional tycoon layer. All durations are game days; no real-world banking simulation.
    public sealed class StationMarketingBank249 : MonoBehaviour
    {
        public static StationMarketingBank249 I { get; private set; }
        public enum Channel { SocialNetworks, Television }
        [Serializable] public class Campaign { public Channel channel; public int startDay, endDay; public float dailyAppeal; }
        [Serializable] public class Loan { public float remaining; public float dailyPayment; public int lastPaymentDay; }
        public readonly List<Campaign> Campaigns = new List<Campaign>();
        public readonly List<Loan> Loans = new List<Loan>();
        public float ActiveAppeal { get; private set; } // Integration point for visitor-arrival balancing.
        private bool visible, confirmCampaign, confirmLoan;
        private Channel selectedChannel;
        private int campaignDays = 3, loanTerm = 10, lastProcessedDay = -1;
        private float loanAmount = 100000f;
        private Rect window = new Rect(30, 90, 430, 530);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            if (FindObjectOfType<StationMarketingBank249>() == null)
                new GameObject("SkiStation_MarketingBank249").AddComponent<StationMarketingBank249>();
        }
        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
        }
        private int Day => GameClock.I != null ? GameClock.I.CurrentDay : 1;
        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.M)) visible = !visible;
            int day = Day;
            if (lastProcessedDay < 0) lastProcessedDay = day;
            while (lastProcessedDay < day) ProcessDay(++lastProcessedDay);
            float appeal = 0f;
            foreach (var c in Campaigns) if (day >= c.startDay && day < c.endDay) appeal += c.dailyAppeal;
            ActiveAppeal = Mathf.Min(1f, appeal);
        }
        public float CampaignCost(Channel channel, int days)
        {
            return Mathf.Clamp(days, 1, 30) * (channel == Channel.Television ? 18000f : 4500f);
        }
        public bool StartCampaign(Channel channel, int days)
        {
            if (days < 1 || days > 30 || StationRuntime.I == null) return false;
            float cost = CampaignCost(channel, days);
            if (!StationRuntime.I.Spend(cost, FinanceCategory.Marketing, "campaign", channel.ToString())) return false;
            Campaigns.Add(new Campaign { channel = channel, startDay = Day, endDay = Day + days,
                dailyAppeal = channel == Channel.Television ? .22f : .08f });
            return true;
        }
        public bool Borrow(float amount, int days)
        {
            if (StationRuntime.I == null || amount < 10000f || amount > 20000000f || days < 5 || days > 30 || Loans.Count >= 3) return false;
            // Fixed 10% premium, disclosed in confirmation; loans are available even with negative cash.
            Loans.Add(new Loan { remaining = amount * 1.10f, dailyPayment = amount * 1.10f / days, lastPaymentDay = Day });
            StationRuntime.I.Earn(amount, FinanceCategory.Other, sourceId: "bank-loan", note: "Capital emprunte (pas un revenu commercial)");
            return true;
        }
        private void ProcessDay(int day)
        {
            for (int i = Loans.Count - 1; i >= 0; --i)
            {
                Loan loan = Loans[i];
                if (loan.lastPaymentDay >= day) continue;
                loan.lastPaymentDay = day;
                float due = Mathf.Min(loan.dailyPayment, loan.remaining);
                // An overdue payment waits until sufficient funds are available; no negative balance is forced.
                if (StationRuntime.I != null && StationRuntime.I.Spend(due, FinanceCategory.Other, "bank-loan", "Remboursement"))
                    loan.remaining = Mathf.Max(0f, loan.remaining - due);
                if (loan.remaining < .01f) Loans.RemoveAt(i);
            }
            Campaigns.RemoveAll(c => c.endDay <= day);
        }
        private void OnGUI() { if (visible) window = GUI.Window(24924, window, DrawWindow, "Marketing & Banque — Ctrl+M"); }
        private void DrawWindow(int id)
        {
            GUILayout.Label("Tresorerie : " + (StationRuntime.I != null ? StationRuntime.I.State.money.ToString("N0") : "—") + " €");
            GUILayout.Label("PUBLICITE (duree en jours de jeu)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reseaux sociaux")) { selectedChannel = Channel.SocialNetworks; confirmCampaign = false; }
            if (GUILayout.Button("Television")) { selectedChannel = Channel.Television; confirmCampaign = false; }
            GUILayout.EndHorizontal();
            GUILayout.Label("Canal : " + (selectedChannel == Channel.Television ? "Television" : "Reseaux sociaux"));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", GUILayout.Width(40))) campaignDays = Mathf.Max(1, campaignDays - 1);
            GUILayout.Label(campaignDays + " jours");
            if (GUILayout.Button("+", GUILayout.Width(40))) campaignDays = Mathf.Min(30, campaignDays + 1);
            GUILayout.EndHorizontal();
            GUILayout.Label("Cout total : " + CampaignCost(selectedChannel, campaignDays).ToString("N0") + " €");
            if (!confirmCampaign) { if (GUILayout.Button("Previsualiser la campagne")) confirmCampaign = true; }
            else
            {
                GUILayout.Label("Confirmer cette campagne ? Cout immediat, effet temporaire.");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Confirmer")) { if (StartCampaign(selectedChannel, campaignDays)) confirmCampaign = false; }
                if (GUILayout.Button("Modifier / Annuler")) confirmCampaign = false;
                GUILayout.EndHorizontal();
            }
            GUILayout.Label("Campagnes actives : " + Campaigns.Count + " | Bonus potentiel : " + Mathf.RoundToInt(ActiveAppeal * 100f) + "%");
            GUILayout.Space(12);
            GUILayout.Label("BANQUE (maximum 3 emprunts simultanes)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("- 10 000")) loanAmount = Mathf.Max(10000f, loanAmount - 10000f);
            GUILayout.Label(loanAmount.ToString("N0") + " €");
            if (GUILayout.Button("+ 10 000")) loanAmount = Mathf.Min(20000000f, loanAmount + 10000f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("- 1 000 000")) loanAmount = Mathf.Max(10000f, loanAmount - 1000000f);
            if (GUILayout.Button("+ 1 000 000")) loanAmount = Mathf.Min(20000000f, loanAmount + 1000000f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("- 5 jours")) loanTerm = Mathf.Max(5, loanTerm - 5);
            GUILayout.Label(loanTerm + " jours");
            if (GUILayout.Button("+ 5 jours")) loanTerm = Mathf.Min(30, loanTerm + 5);
            GUILayout.EndHorizontal();
            GUILayout.Label("A rembourser : " + (loanAmount * 1.1f).ToString("N0") + " € | /jour : " + (loanAmount * 1.1f / loanTerm).ToString("N0") + " €");
            if (!confirmLoan) { if (GUILayout.Button("Previsualiser l'emprunt")) confirmLoan = true; }
            else
            {
                GUILayout.Label("Confirmer ? Prime fixe 10 %. Echeances reportees si fonds insuffisants.");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Confirmer")) { if (Borrow(loanAmount, loanTerm)) confirmLoan = false; }
                if (GUILayout.Button("Modifier / Annuler")) confirmLoan = false;
                GUILayout.EndHorizontal();
            }
            float outstanding = 0f; foreach (var loan in Loans) outstanding += loan.remaining;
            GUILayout.Label("Emprunts : " + Loans.Count + " | Restant : " + outstanding.ToString("N0") + " €");
            if (GUILayout.Button("Fermer")) visible = false;
            GUI.DragWindow(new Rect(0, 0, 430, 22));
        }
    }
}
