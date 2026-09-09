package com.sidsrh.mobile.ui;

import android.app.Activity;
import android.graphics.Color;
import android.graphics.Typeface;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;

public final class Ui {
    private Ui() {}

    public static LinearLayout screenWithBottomBack(Activity a, View content, Runnable onBack) {
        LinearLayout root = new LinearLayout(a);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setLayoutParams(new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));
        root.setBackgroundColor(Color.parseColor("#EEF6FB"));

        ScrollView sv = new ScrollView(a);
        LinearLayout.LayoutParams sp = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, 0, 1f);
        sv.setLayoutParams(sp);
        sv.addView(content);
        root.addView(sv);

        LinearLayout bar = new LinearLayout(a);
        bar.setOrientation(LinearLayout.HORIZONTAL);
        bar.setPadding(24, 16, 24, 24);
        bar.setBackgroundColor(Color.parseColor("#082c60"));
        Button back = new Button(a);
        back.setText("← Back");
        back.setAllCaps(false);
        back.setOnClickListener(v -> onBack.run());
        LinearLayout.LayoutParams bp = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        back.setLayoutParams(bp);
        bar.addView(back);
        root.addView(bar);
        return root;
    }

    public static TextView title(Activity a, String t) {
        TextView tv = new TextView(a);
        tv.setText(t);
        tv.setTextSize(22);
        tv.setTypeface(null, Typeface.BOLD);
        tv.setTextColor(Color.parseColor("#082c60"));
        tv.setPadding(0, 0, 0, 16);
        return tv;
    }

    public static TextView body(Activity a, String t) {
        TextView tv = new TextView(a);
        tv.setText(t);
        tv.setTextSize(15);
        tv.setTextColor(Color.parseColor("#223"));
        tv.setPadding(0, 0, 0, 12);
        return tv;
    }
}
