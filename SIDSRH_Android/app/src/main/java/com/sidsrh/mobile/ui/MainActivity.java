package com.sidsrh.mobile.ui;

import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.widget.Button;
import android.widget.LinearLayout;
import androidx.appcompat.app.AppCompatActivity;

public class MainActivity extends AppCompatActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        SharedPreferences sp = getSharedPreferences("sidsrh", MODE_PRIVATE);
        String role = sp.getString("role", "Patient");
        String name = sp.getString("name", "");

        LinearLayout content = new LinearLayout(this);
        content.setOrientation(LinearLayout.VERTICAL);
        content.setPadding(40, 40, 40, 40);
        content.addView(Ui.title(this, "SIDSRH"));
        content.addView(Ui.body(this, name + "\n" + role + "\n\nSame server/database as the website."));

        Button del = new Button(this);
        del.setText("Deliveries — view full details");
        del.setOnClickListener(v -> startActivity(new Intent(this, DeliveryListActivity.class)));
        content.addView(del);

        Button conf = new Button(this);
        conf.setText("Confirm codes (with details)");
        conf.setOnClickListener(v -> startActivity(new Intent(this, ConfirmActivity.class)));
        content.addView(conf);

        Button out = new Button(this);
        out.setText("Logout");
        out.setOnClickListener(v -> {
            sp.edit().clear().apply();
            startActivity(new Intent(this, WelcomeActivity.class));
            finish();
        });
        content.addView(out);

        setContentView(Ui.screenWithBottomBack(this, content, () -> {
            // bottom back on home = logout to welcome
            sp.edit().clear().apply();
            startActivity(new Intent(this, WelcomeActivity.class));
            finish();
        }));
    }
}
