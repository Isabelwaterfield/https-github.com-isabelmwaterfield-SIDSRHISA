package com.sidsrh.mobile.ui;

import android.content.Intent;
import android.graphics.Color;
import android.os.Bundle;
import android.view.Gravity;
import android.widget.Button;
import android.widget.ImageView;
import android.widget.LinearLayout;
import androidx.appcompat.app.AppCompatActivity;
import com.sidsrh.mobile.R;

public class WelcomeActivity extends AppCompatActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setBackgroundColor(Color.parseColor("#061a38"));
        root.setGravity(Gravity.CENTER_HORIZONTAL);

        ImageView banner = new ImageView(this);
        try { banner.setImageResource(R.drawable.sidsrh_logo); } catch (Exception ignored) {}
        banner.setAdjustViewBounds(true);
        banner.setScaleType(ImageView.ScaleType.FIT_CENTER);
        LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT, LinearLayout.LayoutParams.WRAP_CONTENT);
        lp.setMargins(12, 24, 12, 12);
        banner.setLayoutParams(lp);
        root.addView(banner);

        Button login = new Button(this);
        login.setText("Login");
        login.setAllCaps(false);
        login.setOnClickListener(v -> startActivity(new Intent(this, LoginActivity.class)));
        root.addView(login);

        setContentView(root);
    }
}
