package com.sidsrh.mobile.ui;

import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.Toast;
import androidx.appcompat.app.AppCompatActivity;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import com.sidsrh.mobile.data.ApiClient;
import java.util.concurrent.Executors;

public class LoginActivity extends AppCompatActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        LinearLayout content = new LinearLayout(this);
        content.setOrientation(LinearLayout.VERTICAL);
        content.setPadding(40, 40, 40, 40);
        content.addView(Ui.title(this, "Sign in"));
        content.addView(Ui.body(this, "Uses the same database as the web API.\nAPI: " + ApiClient.baseUrl()));

        EditText email = new EditText(this);
        email.setHint("Email");
        email.setText("");
        content.addView(email);

        EditText pass = new EditText(this);
        pass.setHint("Password");
        pass.setText("");
        content.addView(pass);

        Button btn = new Button(this);
        btn.setText("Sign In");
        btn.setOnClickListener(v -> {
            btn.setEnabled(false);
            Executors.newSingleThreadExecutor().execute(() -> {
                try {
                    String json = "{\"email\":\"" + email.getText().toString().trim()
                            + "\",\"password\":\"" + pass.getText().toString() + "\"}";
                    String body = ApiClient.post("api/Auth/login", json, null);
                    JsonObject o = JsonParser.parseString(body).getAsJsonObject();
                    String token = o.get("token").getAsString();
                    JsonObject u = o.getAsJsonObject("user");
                    getSharedPreferences("sidsrh", MODE_PRIVATE).edit()
                            .putString("token", token)
                            .putString("role", u.get("role").getAsString())
                            .putString("name", u.get("firstName").getAsString() + " " + u.get("lastName").getAsString())
                            .apply();
                    new Handler(Looper.getMainLooper()).post(() -> {
                        startActivity(new Intent(this, MainActivity.class));
                        finish();
                    });
                } catch (Exception e) {
                    new Handler(Looper.getMainLooper()).post(() -> {
                        btn.setEnabled(true);
                        Toast.makeText(this, "Login failed. Is the API running?\n" + e.getMessage(), Toast.LENGTH_LONG).show();
                    });
                }
            });
        });
        content.addView(btn);

        setContentView(Ui.screenWithBottomBack(this, content, this::finish));
    }
}
