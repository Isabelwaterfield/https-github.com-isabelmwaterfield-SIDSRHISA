package com.sidsrh.mobile.data;

import com.sidsrh.mobile.BuildConfig;
import okhttp3.MediaType;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.RequestBody;
import okhttp3.Response;

public class ApiClient {
    private static final OkHttpClient client = new OkHttpClient();
    private static final MediaType JSON = MediaType.get("application/json; charset=utf-8");

    public static String baseUrl() {
        return BuildConfig.API_BASE_URL;
    }

    public static String post(String path, String json, String token) throws Exception {
        Request.Builder b = new Request.Builder()
                .url(baseUrl() + path)
                .post(RequestBody.create(json, JSON));
        if (token != null && !token.isEmpty()) b.header("Authorization", "Bearer " + token);
        try (Response r = client.newCall(b.build()).execute()) {
            String body = r.body() != null ? r.body().string() : "";
            if (!r.isSuccessful()) throw new Exception(body.isEmpty() ? ("HTTP " + r.code()) : body);
            return body;
        }
    }

    public static String get(String path, String token) throws Exception {
        Request.Builder b = new Request.Builder().url(baseUrl() + path).get();
        if (token != null && !token.isEmpty()) b.header("Authorization", "Bearer " + token);
        try (Response r = client.newCall(b.build()).execute()) {
            String body = r.body() != null ? r.body().string() : "";
            if (!r.isSuccessful()) throw new Exception(body.isEmpty() ? ("HTTP " + r.code()) : body);
            return body;
        }
    }
}
