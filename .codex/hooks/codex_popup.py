import sys
import os
import json
import traceback

import tkinter as tk

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ERROR_LOG = os.path.join(SCRIPT_DIR, 'codex_popup_error.log')
RAW_LOG = os.path.join(SCRIPT_DIR, 'codex_popup_raw.log')
BRAND = 'Codex'

# cross-platform fonts
if sys.platform == 'win32':
    FONT = 'Microsoft YaHei UI'
    MONO = 'Consolas'
elif sys.platform == 'darwin':
    FONT = 'Helvetica Neue'
    MONO = 'Menlo'
else:
    FONT = 'Noto Sans'
    MONO = 'DejaVu Sans Mono'

# 薰衣草紫主题（参考设计稿）
C = {
    'bg':       '#faf9ff',   # 极浅薰衣草白
    'card':     '#ffffff',
    'card2':    '#f3f0ff',   # 浅紫卡片底（详情/选项）
    'text':     '#2b2440',   # 深紫黑
    'muted':    '#9a93b8',   # 次要灰紫
    'accent':   '#8b7cf6',   # 主紫
    'accent2':  '#7c6cf0',   # 主紫·深
    'remember': '#a78bfa',   # 记住·亮紫
    'remember2':'#9575f0',
    'danger':   '#f472a8',   # 拒绝·粉
    'danger2':  '#ec5f99',
    'border':   '#e7e1fb',   # 浅紫描边
    'bar_bg':   '#faf9ff',
    'track':    '#ece7fb',   # 进度条轨道
    'badge_bg': '#8b7cf6',   # 默认徽章紫
    'badge_fg': '#ffffff',
}

# 彩虹渐变顶条的色站（粉→橙→黄→绿→青→蓝→紫）
RAINBOW = ['#ff8fb1', '#ffb38a', '#ffe08a', '#9ae6a3',
           '#8ad7e6', '#8aaff0', '#b89af0', '#e0a0e8']

# 工具名 → 徽章底色
BADGE_COLORS = {
    'Bash':         '#8b7cf6',
    'Write':        '#34d399',
    'Edit':         '#34d399',
    'MultiEdit':    '#34d399',
    'Read':         '#60a5fa',
    'ExitPlanMode': '#a78bfa',
    'AskUserQuestion': '#a78bfa',
}

# 超时行为
AUTO_ALLOW_SEC = 30        # 权限弹窗：N 秒无操作自动同意


def _center(root, w, h):
    sw = root.winfo_screenwidth()
    sh = root.winfo_screenheight()
    root.geometry(f'{w}x{h}+{(sw-w)//2}+{(sh-h)//2}')


def _focus(root, default_btn=None):
    root.lift()
    root.attributes('-topmost', True)
    root.after_idle(root.attributes, '-topmost', False)
    root.focus_force()
    if default_btn is not None:
        try:
            default_btn.focus_set()
        except Exception:
            pass


def _hex2rgb(h):
    h = h.lstrip('#')
    return tuple(int(h[i:i+2], 16) for i in (0, 2, 4))


def _rgb2hex(rgb):
    return '#%02x%02x%02x' % rgb


def rainbow_bar(parent, width, height=5):
    """绘制彩虹渐变顶条。"""
    cv = tk.Canvas(parent, width=width, height=height, bd=0,
                   highlightthickness=0, bg=C['bg'])
    stops = RAINBOW
    seg = max(1, len(stops) - 1)
    for x in range(width):
        pos = x / max(1, width - 1) * seg
        i = min(seg - 1, int(pos))
        f = pos - i
        a, b = _hex2rgb(stops[i]), _hex2rgb(stops[i + 1])
        col = _rgb2hex(tuple(int(a[k] + (b[k] - a[k]) * f) for k in range(3)))
        cv.create_line(x, 0, x, height, fill=col)
    return cv


def round_badge(parent, text, bg, fg='#ffffff', pad_x=12, pad_y=5, font_size=11):
    """彩色圆角工具徽章。"""
    f = (FONT, font_size, 'bold')
    probe = tk.Label(parent, text=text, font=f)
    probe.update_idletasks()
    tw = probe.winfo_reqwidth()
    th = probe.winfo_reqheight()
    probe.destroy()
    w, h = tw + pad_x * 2, th + pad_y * 2
    r = h // 2
    cv = tk.Canvas(parent, width=w, height=h, bd=0,
                   highlightthickness=0, bg=parent['bg'])
    cv.create_oval(0, 0, 2 * r, h, fill=bg, outline=bg)
    cv.create_oval(w - 2 * r, 0, w, h, fill=bg, outline=bg)
    cv.create_rectangle(r, 0, w - r, h, fill=bg, outline=bg)
    cv.create_text(w // 2, h // 2, text=text, fill=fg, font=f)
    return cv


class RoundButton(tk.Canvas):
    """圆角按钮：filled=实心，否则描边；hover 变色。"""
    def __init__(self, parent, text, command, color, color2,
                 fg='#ffffff', filled=True, width=None, font_size=10):
        self._f = (FONT, font_size, 'bold')
        self._text = text
        probe = tk.Label(parent, text=text, font=self._f)
        probe.update_idletasks()
        tw = probe.winfo_reqwidth()
        probe.destroy()
        w = width or (tw + 40)
        h = 38
        super().__init__(parent, width=w, height=h, bd=0,
                         highlightthickness=0, bg=parent['bg'], cursor='hand2')
        self._color, self._color2 = color, color2
        self._fg, self._filled = fg, filled
        self._bw, self._bh = w, h   # 不能用 _w/_h：tkinter 内部占用 _w
        self._draw(color)
        self.bind('<Button-1>', lambda e: command())
        self.bind('<Enter>', lambda e: self._draw(color2))
        self.bind('<Leave>', lambda e: self._draw(color))

    def _round_rect(self, x1, y1, x2, y2, r, **kw):
        pts = [x1+r, y1, x2-r, y1, x2, y1, x2, y1+r, x2, y2-r, x2, y2,
               x2-r, y2, x1+r, y2, x1, y2, x1, y2-r, x1, y1+r, x1, y1]
        return self.create_polygon(pts, smooth=True, **kw)

    def _draw(self, c):
        self.delete('all')
        w, h, r = self._bw, self._bh, 10
        if self._filled:
            self._round_rect(1, 1, w-1, h-1, r, fill=c, outline=c)
            tc = self._fg
        else:
            self._round_rect(1, 1, w-1, h-1, r, fill=C['card'], outline=c, width=1)
            tc = c
        self.create_text(w//2, h//2, text=self._text, fill=tc, font=self._f)


def _extract_detail(tool_input):
    lines = []
    for k, v in tool_input.items():
        if k == 'description':
            continue
        if isinstance(v, str):
            lines.append(f'{k}: {v}')
        elif isinstance(v, (int, float, bool)):
            lines.append(f'{k}: {v}')
        elif isinstance(v, dict):
            lines.append(f'{k}: {json.dumps(v, ensure_ascii=False)}')
        elif isinstance(v, list):
            lines.append(f'{k}: {json.dumps(v, ensure_ascii=False)}')
    return '\n'.join(lines)


def _short_button_text(text, max_len=14):
    text = str(text or '').strip()
    if len(text) <= max_len:
        return text
    return text[:max_len - 1] + '…'


def _permission_suggestion_label(sugg, index):
    if not isinstance(sugg, dict):
        return f'记住选项 {index + 1}'

    for key in ('label', 'title', 'name'):
        value = sugg.get(key)
        if isinstance(value, str) and value.strip():
            return _short_button_text(value)

    rules = sugg.get('rules', [])
    if isinstance(rules, list) and len(rules) == 1:
        rule = rules[0]
        if isinstance(rule, dict):
            tool = rule.get('toolName') or rule.get('tool_name') or rule.get('tool')
            pattern = rule.get('command') or rule.get('pattern') or rule.get('rule')
            if tool and pattern:
                return _short_button_text(f'记住 {tool}: {pattern}')
            if tool:
                return _short_button_text(f'记住 {tool}')
        elif isinstance(rule, str) and rule.strip():
            return _short_button_text(f'记住 {rule}')

    if isinstance(rules, list) and len(rules) > 1:
        return f'记住 {len(rules)} 条规则'

    return f'记住选项 {index + 1}'


def _extract_plan(tool_input):
    """提取 ExitPlanMode 的方案正文；不同版本字段名可能不同，依次尝试。"""
    for key in ('plan', 'content', 'message', 'text'):
        v = tool_input.get(key)
        if isinstance(v, str) and v.strip():
            return v
    return ''


def show_permission_dialog(data):
    tool_name = data.get('tool_name', 'Unknown')
    tool_input = data.get('tool_input', {})
    suggestions = data.get('permission_suggestions', [])

    # ExitPlanMode = Claude 请求批准实施方案，切换为"方案审批"展示
    is_plan = tool_name == 'ExitPlanMode'
    title_desc = tool_input.get('description', '') if isinstance(tool_input, dict) else ''
    body_text = _extract_plan(tool_input) if is_plan else ''
    if not body_text:
        body_text = _extract_detail(tool_input)

    decision = {'behavior': 'deny'}

    root = tk.Tk()
    root.title('Codex')
    root.resizable(False, False)
    root.configure(bg=C['bg'])

    win_w = 700 if is_plan else (720 if len(suggestions) > 1 else 560)
    win_h = 560 if is_plan else 330
    _center(root, win_w, win_h)

    rainbow_bar(root, win_w).pack(fill='x')

    # ── 标题行：徽章 + 文案 ──
    head = tk.Frame(root, bg=C['bg'])
    head.pack(fill='x', padx=22, pady=(16, 6))
    badge_c = BADGE_COLORS.get(tool_name, C['badge_bg'])
    round_badge(head, tool_name, badge_c).pack(side='left')
    tk.Label(head, text='请求执行操作', font=(FONT, 14, 'bold'),
             fg=C['text'], bg=C['bg']).pack(side='left', padx=(12, 0))

    # ── 描述卡（浅紫，居中加粗，带 emoji 风格）──
    if title_desc:
        desc_card = tk.Frame(root, bg=C['card2'],
                             highlightbackground=C['border'], highlightthickness=1)
        desc_card.pack(fill='x', padx=18, pady=(6, 8))
        tk.Label(desc_card, text=title_desc, font=(FONT, 12, 'bold'),
                 fg=C['accent2'], bg=C['card2'], wraplength=win_w - 80,
                 justify='center', padx=14, pady=12).pack(fill='x')

    # ── 详情卡（命令/参数 或 方案正文）──
    if body_text:
        detail_card = tk.Frame(root, bg=C['card2'],
                               highlightbackground=C['border'], highlightthickness=1)
        detail_card.pack(fill='both', expand=is_plan, padx=18, pady=(0, 8))

        text_h = 20 if is_plan else 3
        detail_text = tk.Text(detail_card, font=(MONO, 11),
                              fg=C['text'], bg=C['card2'],
                              wrap='word', height=text_h, bd=0, padx=12, pady=10,
                              cursor='arrow', relief='flat')
        detail_text.insert('1.0', body_text)
        detail_text.configure(state='disabled')
        detail_text.pack(side='left', fill='both', expand=True)

        scrollbar = tk.Scrollbar(detail_card, command=detail_text.yview, bd=0,
                                 elementborderwidth=0, highlightthickness=0)
        scrollbar.pack(side='right', fill='y')
        detail_text.configure(yscrollcommand=scrollbar.set)

    # ── 等待进度条 + 秒数 ──
    prog_row = tk.Frame(root, bg=C['bg'])
    prog_row.pack(fill='x', padx=20, pady=(2, 4))
    # 标签先占住右侧固定空间，进度条再吃剩余宽度，避免文字被挤出
    sec_lbl = tk.Label(prog_row, text=f'{AUTO_ALLOW_SEC}s 后自动同意',
                       font=(FONT, 9), fg=C['muted'], bg=C['bg'], width=12, anchor='e')
    sec_lbl.pack(side='right', padx=(8, 0))
    prog = tk.Canvas(prog_row, height=6, bd=0,
                     highlightthickness=0, bg=C['bg'])
    prog.pack(side='left', fill='x', expand=True)

    timer = {'left': AUTO_ALLOW_SEC}

    def _redraw():
        w = prog.winfo_width()
        if w <= 1:
            return
        prog.delete('all')
        prog.create_rectangle(0, 0, w, 6, fill=C['track'], outline=C['track'])
        fw = max(0, int(w * timer['left'] / AUTO_ALLOW_SEC))
        prog.create_rectangle(0, 0, fw, 6, fill=C['accent'], outline=C['accent'])

    prog.bind('<Configure>', lambda e: _redraw())

    def tick():
        if not prog.winfo_exists():
            return
        timer['left'] -= 1
        if timer['left'] <= 0:
            on_allow_once()   # 30 秒无操作 → 自动同意
            return
        sec_lbl.config(text=f"{timer['left']}s 后自动同意")
        _redraw()
        root.after(1000, tick)

    def on_allow_once():
        decision['behavior'] = 'allow'
        root.destroy()

    def on_allow_rule(sugg):
        decision['behavior'] = 'allow'
        decision['updatedPermissions'] = [sugg]
        root.destroy()

    def on_deny():
        root.destroy()

    # ── 按钮栏（右对齐：同意描边 / 权限建议实心紫 / 拒绝实心粉）──
    bar = tk.Frame(root, bg=C['bg'])
    bar.pack(fill='x', side='bottom', padx=20, pady=(4, 16))

    RoundButton(bar, '拒绝', on_deny, C['danger'], C['danger2'],
                filled=True).pack(side='right', padx=(8, 0))

    if not is_plan:
        if not suggestions:
            suggestions = [{
                'type': 'addRules',
                'rules': [{'toolName': tool_name}],
                'behavior': 'allow',
                'destination': 'localSettings',
            }]
        for idx, suggestion in reversed(list(enumerate(suggestions))):
            label = _permission_suggestion_label(suggestion, idx)
            RoundButton(bar, label, lambda s=suggestion: on_allow_rule(s),
                        C['remember'], C['remember2'], filled=True).pack(
                side='right', padx=(8, 0))

    btn_allow = RoundButton(bar, '批准方案' if is_plan else '同意', on_allow_once,
                            C['accent'], C['accent2'], fg=C['accent'],
                            filled=False)
    btn_allow.pack(side='right')

    _focus(root, None)
    root.bind('<Escape>', lambda e: on_deny())
    root.bind('<Return>', lambda e: on_allow_once())
    tick()

    root.mainloop()
    return decision


def show_question_dialog(data):
    """AskUserQuestion：展示问题与选项，让用户在弹窗中选择。

    PermissionRequest 只能 allow/deny，无法直接回传所选答案；
    因此把所选项作为 deny 的理由文本回传，Claude 读到后按所选继续。
    返回 (behavior, reason)。"""
    tool_input = data.get('tool_input', {})
    questions = tool_input.get('questions', []) or []

    # 记录每个问题对应的选择变量
    state = []  # [(question_obj, [tk.IntVar...], multiSelect)]
    result = {'behavior': 'deny', 'reason': ''}

    root = tk.Tk()
    root.title('Codex')
    root.resizable(False, False)
    root.configure(bg=C['bg'])

    # 固定窗口高度，内容超出时由中间区滚动，底部按钮栏永不被压缩
    opt_total = sum(len(q.get('options', []) or []) for q in questions)
    content_h = 70 + len(questions) * 60 + opt_total * 40 + 96
    win_w = 580
    win_h = min(680, 150 + content_h)   # 顶条+标题+底栏的固定占用
    _center(root, win_w, win_h)

    # 四行栅格：顶条 / 标题(固定) / 内容(可伸缩+滚动) / 底部栏(固定)
    root.grid_columnconfigure(0, weight=1)
    root.grid_rowconfigure(2, weight=1)

    rainbow_bar(root, win_w).grid(row=0, column=0, sticky='ew')

    header = tk.Frame(root, bg=C['bg'])
    header.grid(row=1, column=0, sticky='ew', padx=22, pady=(14, 8))
    hrow = tk.Frame(header, bg=C['bg'])
    hrow.pack(anchor='w')
    round_badge(hrow, 'Ask', C['remember']).pack(side='left')
    tk.Label(hrow, text='需要你做选择', font=(FONT, 14, 'bold'),
             fg=C['text'], bg=C['bg']).pack(side='left', padx=(12, 0))
    tk.Label(header, text='选择一个选项，或自行输入 / 就此展开讨论',
             font=(FONT, 9), fg=C['muted'], bg=C['bg']).pack(anchor='w', pady=(4, 0))

    # ── 可滚动内容区 ──────────────────────────────────────────
    container = tk.Frame(root, bg=C['bg'])
    container.grid(row=2, column=0, sticky='nsew', padx=(18, 0))

    canvas = tk.Canvas(container, bg=C['bg'], bd=0, highlightthickness=0)
    vbar = tk.Scrollbar(container, command=canvas.yview, bd=0,
                        elementborderwidth=0, highlightthickness=0)
    canvas.configure(yscrollcommand=vbar.set)
    vbar.pack(side='right', fill='y')
    canvas.pack(side='left', fill='both', expand=True)

    body = tk.Frame(canvas, bg=C['bg'])
    body_id = canvas.create_window((0, 0), window=body, anchor='nw')

    def _sync_scrollregion(_=None):
        canvas.configure(scrollregion=canvas.bbox('all'))

    def _sync_width(e):
        canvas.itemconfig(body_id, width=e.width)

    body.bind('<Configure>', _sync_scrollregion)
    canvas.bind('<Configure>', _sync_width)

    def _on_wheel(e):
        canvas.yview_scroll(int(-e.delta / 120), 'units')

    canvas.bind_all('<MouseWheel>', _on_wheel)

    for q in questions:
        q_text = q.get('question', '')
        options = q.get('options', []) or []
        multi = bool(q.get('multiSelect', False))

        card = tk.Frame(body, bg=C['card2'], padx=16, pady=12,
                        highlightbackground=C['border'], highlightthickness=1)
        card.pack(fill='x', pady=(0, 10), padx=(0, 18))

        tk.Label(card, text=q_text, font=(FONT, 11, 'bold'),
                 fg=C['text'], bg=C['card2'], wraplength=500,
                 justify='left').pack(anchor='w', pady=(0, 8))

        vars_ = []
        if multi:
            for opt in options:
                v = tk.IntVar(value=0)
                vars_.append(v)
                label = opt.get('label', '')
                desc = opt.get('description', '')
                row = tk.Frame(card, bg=C['card2'])
                row.pack(anchor='w', fill='x', pady=2)
                cb = tk.Checkbutton(row, text=label, variable=v,
                               font=(FONT, 10, 'bold'), fg=C['text'], bg=C['card2'],
                               activebackground=C['card2'], selectcolor=C['card'],
                               anchor='w', cursor='hand2', bd=0, highlightthickness=0)
                cb.pack(anchor='w', fill='x')
                if desc:
                    tk.Label(row, text=desc, font=(FONT, 9), fg=C['muted'],
                             bg=C['card2'], wraplength=470, justify='left').pack(
                        anchor='w', padx=(22, 0))
        else:
            sel = tk.IntVar(value=-1)
            vars_.append(sel)
            for i, opt in enumerate(options):
                label = opt.get('label', '')
                desc = opt.get('description', '')
                row = tk.Frame(card, bg=C['card2'])
                row.pack(anchor='w', fill='x', pady=2)
                rb = tk.Radiobutton(row, text=label, variable=sel, value=i,
                               font=(FONT, 10, 'bold'), fg=C['text'], bg=C['card2'],
                               activebackground=C['card2'], selectcolor=C['accent'],
                               anchor='w', cursor='hand2', bd=0, highlightthickness=0)
                rb.pack(anchor='w', fill='x')
                if desc:
                    tk.Label(row, text=desc, font=(FONT, 9), fg=C['muted'],
                             bg=C['card2'], wraplength=470, justify='left').pack(
                        anchor='w', padx=(22, 0))

        state.append((q, vars_, multi))

    # 「Type something」入口：自定义文字输入，可替代/补充选项
    input_card = tk.Frame(body, bg=C['card2'], padx=16, pady=12,
                          highlightbackground=C['border'], highlightthickness=1)
    input_card.pack(fill='x', pady=(0, 10), padx=(0, 18))
    tk.Label(input_card, text='或自己输入（Type something）',
             font=(FONT, 10, 'bold'), fg=C['text'], bg=C['card2']).pack(anchor='w', pady=(0, 6))
    custom_entry = tk.Entry(input_card, font=(FONT, 10), fg=C['text'], bg=C['card'],
                            relief='flat', bd=0,
                            highlightbackground=C['border'], highlightthickness=1)
    custom_entry.pack(fill='x', ipady=5)

    def collect():
        parts = []
        for q, vars_, multi in state:
            opts = q.get('options', []) or []
            header_txt = q.get('header') or q.get('question', '')
            if multi:
                chosen = [opts[i].get('label', '')
                          for i, v in enumerate(vars_) if v.get()]
                parts.append(f'{header_txt}: {", ".join(chosen) if chosen else "(未选)"}')
            else:
                idx = vars_[0].get()
                label = opts[idx].get('label', '') if 0 <= idx < len(opts) else '(未选)'
                parts.append(f'{header_txt}: {label}')
        return ' | '.join(parts)

    def _close():
        canvas.unbind_all('<MouseWheel>')
        root.destroy()

    def on_confirm():
        result['behavior'] = 'deny'  # 通过 reason 回传所选
        msg = '用户已通过弹窗选择：' + collect()
        custom = custom_entry.get().strip()
        if custom:
            msg += f'｜补充输入：{custom}'
        result['reason'] = msg
        _close()

    def on_chat():
        # 「Chat about this」：不选选项，请 Claude 就此展开讨论
        result['behavior'] = 'deny'
        result['reason'] = '用户希望就此问题展开讨论（Chat about this），暂不直接选择。'
        _close()

    def on_cancel():
        result['behavior'] = 'deny'
        result['reason'] = '用户在选择弹窗中取消。'
        _close()

    sep = tk.Frame(root, bg=C['border'], height=1)
    sep.grid(row=3, column=0, sticky='ew')
    bar = tk.Frame(root, bg=C['bg'], padx=18, pady=12)
    bar.grid(row=4, column=0, sticky='ew')

    RoundButton(bar, '取消', on_cancel, C['danger'], C['danger2'],
                filled=True).pack(side='right', padx=(8, 0))
    RoundButton(bar, '确认选择', on_confirm, C['accent'], C['accent2'],
                filled=True).pack(side='right', padx=(8, 0))
    RoundButton(bar, '就此讨论', on_chat, C['remember'], C['remember2'],
                fg=C['remember'], filled=False).pack(side='left')

    _focus(root, None)
    root.bind('<Escape>', lambda e: on_cancel())
    root.bind('<Return>', lambda e: on_confirm())

    root.mainloop()
    return result['behavior'], result['reason']


# ── entry ──────────────────────────────────────────────────────────────
def main():
    try:
        raw = sys.stdin.buffer.read()
        if not raw:
            return
        data = json.loads(raw.decode())
    except Exception:
        with open(ERROR_LOG, 'a', encoding='utf-8') as f:
            f.write(f'[stdin-error] {traceback.format_exc()}\n')
        return

    # 取证：记录每次 hook 收到的原始 JSON，便于确认 AskUserQuestion 是否触发
    try:
        with open(RAW_LOG, 'a', encoding='utf-8') as f:
            f.write(json.dumps(data, ensure_ascii=False) + '\n')
    except Exception:
        pass

    event = data.get('hook_event_name', '')
    tool_name = data.get('tool_name', '')

    try:
        if event == 'PermissionRequest':
            if tool_name == 'AskUserQuestion':
                behavior, reason = show_question_dialog(data)
                decision = {'behavior': behavior}
                if reason:
                    decision['message'] = reason
            else:
                decision = show_permission_dialog(data)
            output = {
                'hookSpecificOutput': {
                    'hookEventName': 'PermissionRequest',
                    'decision': decision,
                }
            }
            print(json.dumps(output, ensure_ascii=False))
    except Exception:
        with open(ERROR_LOG, 'a', encoding='utf-8') as f:
            f.write(f'[gui-error] {traceback.format_exc()}\n')


if __name__ == '__main__':
    main()
