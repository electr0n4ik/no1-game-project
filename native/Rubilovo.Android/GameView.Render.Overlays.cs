using Android.Graphics;
using Android.Views;
using Rubilovo.Logic;

namespace Rubilovo.Android;

public partial class GameView
{
    private readonly RectF?[] _rowRects = new RectF?[6];

    private void RenderOverlays(Canvas c, float cx, float cy)
    {
        // Boss HP bar
        if (_bossAlive is { Hp: > 0 } boss)
        {
            float bw = Width * 0.62f, bh = 9 * _density;
            float bx = cx - bw / 2, by = 40 * _density;
            _p.SetStyle(Paint.Style.Fill);
            _p.Color = Color.Argb(180, 43, 45, 66);
            c.DrawRoundRect(new RectF(bx, by, bx + bw, by + bh), 4 * _density, 4 * _density, _p);
            _p.Color = Color.Rgb(239, 71, 111);
            float ratio = Math.Clamp(boss.Hp / MathF.Max(1f, boss.MaxHp), 0f, 1f);
            c.DrawRoundRect(new RectF(bx, by, bx + bw * ratio, by + bh), 4 * _density, 4 * _density, _p);
            _p.Color = Color.White;
            _p.TextSize = 11 * _density;
            c.DrawText(boss.Sprite.ToUpper(), cx - _p.MeasureText(boss.Sprite) / 2, by - 4 * _density, _p);
        }

        // Damage numbers
        _p.SetStyle(Paint.Style.Fill);
        foreach (DmgNum n in _nums)
        {
            _p.Color = n.Crit ? Color.Rgb(255, 209, 102) : Color.White;
            _p.TextSize = (n.Crit ? 19 : 14) * _density;
            int a = Math.Clamp((int)(n.Life / 0.6f * 255), 0, 255);
            _p.Alpha = a;
            float sx = Width / 2f + (n.X - _px) * (Height / (OrthoNow * 2f));
            float sy = Height / 2f + (n.Y - _py) * (Height / (OrthoNow * 2f));
            c.DrawText(n.Txt, sx - _p.MeasureText(n.Txt) / 2, sy, _p);
            _p.Alpha = 255;
        }

        // Death particles
        foreach (Part q in _parts)
        {
            _p.Color = Color.Argb(Math.Clamp((int)(q.Life / 0.4f * 255), 0, 255), q.R, q.G, q.B);
            float sx = Width / 2f + (q.X - _px) * (Height / (OrthoNow * 2f));
            float sy = Height / 2f + (q.Y - _py) * (Height / (OrthoNow * 2f));
            c.DrawCircle(sx, sy, 3 * _density, _p);
        }

        // Combo
        if (_runTime < _comboUntil && _comboShown > 0)
        {
            _p.Color = Color.Rgb(255, 209, 102);
            _p.TextSize = 30 * _density;
            string t = $"x{_comboShown * 50} МЯСО!";
            c.DrawText(t, cx - _p.MeasureText(t) / 2, Height * 0.14f, _p);
        }

        // Toast
        if (_runTime < _toastUntil && !string.IsNullOrEmpty(_toast))
        {
            _p.Color = Color.Rgb(6, 214, 160);
            _p.TextSize = 15 * _density;
            c.DrawText(_toast, cx - _p.MeasureText(_toast) / 2, Height * 0.90f, _p);
        }

        if (_phase == Phase.LevelUp) RenderCards(c, cx);
        if (_phase == Phase.Tree) RenderTree(c, cx);
    }

    private void RenderCards(Canvas c, float cx)
    {
        _p.SetStyle(Paint.Style.Fill);
        _p.Color = Color.Argb(200, 20, 21, 38);
        c.DrawRect(0, 0, Width, Height, _p);
        _p.Color = Color.White;
        _p.TextSize = 26 * _density;
        string t = $"УРОВЕНЬ {_level} — ВЫБЕРИ УЛУЧШЕНИЕ";
        c.DrawText(t, cx - _p.MeasureText(t) / 2, Height * 0.26f, _p);

        for (int i = 0; i < _cards.Count && i < 3; i++)
        {
            UpgradeCard card = _cards[i];
            RectF r = _cardRects[i]!;
            _p.SetStyle(Paint.Style.Fill);
            _p.Color = Color.Argb(235, 43, 45, 66);
            c.DrawRoundRect(r, 14 * _density, 14 * _density, _p);
            _p.SetStyle(Paint.Style.Stroke);
            _p.StrokeWidth = 3;
            _p.Color = card.Type switch
            {
                CardType.NewWeapon => Color.Rgb(6, 214, 160),
                CardType.UpgradeWeapon => Color.Rgb(17, 138, 178),
                _ => Color.Rgb(255, 209, 102)
            };
            c.DrawRoundRect(r, 14 * _density, 14 * _density, _p);

            _p.SetStyle(Paint.Style.Fill);
            string head = card.Type switch
            {
                CardType.NewWeapon => "НОВОЕ ОРУЖИЕ",
                CardType.UpgradeWeapon => "УЛУЧШЕНИЕ",
                _ => "ПАССИВКА"
            };
            _p.TextSize = 11 * _density;
            _p.Color = Color.Rgb(184, 189, 212);
            c.DrawText(head, r.CenterX() - _p.MeasureText(head) / 2, r.Top + 24 * _density, _p);

            _p.TextSize = 16 * _density;
            _p.Color = Color.White;
            string name = CardLabel(card);
            c.DrawText(name, r.CenterX() - _p.MeasureText(name) / 2, r.CenterY(), _p);

            _p.TextSize = 12 * _density;
            _p.Color = Color.Rgb(184, 189, 212);
            string foot = card.Type switch
            {
                CardType.UpgradeWeapon => $"ур. {_loadout.WeaponLevels[(int)card.Weapon]} → {_loadout.WeaponLevels[(int)card.Weapon] + 1}",
                CardType.Passive => $"ур. {_loadout.PassiveLevels[(int)card.Passive]} → {_loadout.PassiveLevels[(int)card.Passive] + 1}",
                _ => "в бой!"
            };
            c.DrawText(foot, r.CenterX() - _p.MeasureText(foot) / 2, r.Bottom - 20 * _density, _p);
        }
        _p.SetStyle(Paint.Style.Fill);
    }

    private void RenderTree(Canvas c, float cx)
    {
        _p.SetStyle(Paint.Style.Fill);
        _p.Color = Color.Argb(215, 20, 21, 38);
        c.DrawRect(0, 0, Width, Height, _p);
        _p.Color = Color.White;
        _p.TextSize = 24 * _density;
        string title = $"ДЕРЕВО СТАТОВ · 🍖 {_save.Meat}";
        c.DrawText(title, cx - _p.MeasureText(title) / 2, Height * 0.10f, _p);

        for (int b = 0; b < 6; b++)
        {
            float y = Height * (0.16f + b * 0.095f);
            var r = new RectF(Width * 0.06f, y, Width * 0.94f, y + Height * 0.075f);
            _rowRects[b] = r;

            bool unlocked = TreeUnlocked(b);
            bool maxed = TreeMaxed(b);
            long cost = TreeCost(b);

            _p.SetStyle(Paint.Style.Fill);
            _p.Color = unlocked ? Color.Argb(220, 43, 45, 66) : Color.Argb(110, 43, 45, 66);
            c.DrawRoundRect(r, 12 * _density, 12 * _density, _p);
            _p.SetStyle(Paint.Style.Stroke);
            _p.StrokeWidth = 2;
            _p.Color = unlocked ? Color.Rgb(255, 209, 102) : Color.Rgb(90, 95, 130);
            c.DrawRoundRect(r, 12 * _density, 12 * _density, _p);

            _p.SetStyle(Paint.Style.Fill);
            _p.TextSize = 15 * _density;
            _p.Color = unlocked ? Color.White : Color.Rgb(120, 125, 160);
            string left = $"{GameBalance.Tree_BranchNames[b]}  ·  ур. {_save.TreeSteps[b]}/{GameBalance.Tree_StepsPerBranch}";
            c.DrawText(left, r.Left + 14 * _density, r.CenterY() - 2 * _density, _p);
            _p.TextSize = 13 * _density;
            string right = maxed ? "МАКС" : !unlocked
                ? $"🔒 после {GameBalance.Tree_UnlockAfterTotalSteps[b]} ступеней"
                : $"🍖 {cost} — купить";
            c.DrawText(right, r.Right - 14 * _density - _p.MeasureText(right), r.CenterY() + 12 * _density, _p);
        }

        DrawBtn(c, 8, "НАЗАД");
        _p.SetStyle(Paint.Style.Fill);
    }

    private void HandleTreeTap(MotionEvent e)
    {
        if (InBtn(e, 8)) { _phase = Phase.Menu; return; }
        for (int b = 0; b < 6; b++)
        {
            if (_rowRects[b] == null || !InRect(e, _rowRects[b]!)) continue;
            if (TryBuyBranch(b)) Toast($"+1 {GameBalance.Tree_BranchNames[b]}");
            else Toast(TreeMaxed(b) ? "ветка прокачана" : !TreeUnlocked(b) ? "ветка ещё закрыта" : "не хватает 🍖");
            return;
        }
    }
}
