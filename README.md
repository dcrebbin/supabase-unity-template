# Supabase Unity Template for iOS & Android (with Social Sign-in)

Integrating Supabase into Unity featuring Social Sign-in and client-side supabase updates.

1. Sign in via one of the providers (more is configurable).

![](/Assets/Tutorial/social-sign-in.png)

2. Once signed in via a web client you'll be redirected back to your app via a deeplink.

![](/Assets/Tutorial/signed-in.png)
You can now perform authenticated supabase client requests.

Forked from: https://github.com/wiverson/supabase-unity-template

### Issues:

- Currently not working with Apple Sign In
- Currently only tested to work for iOS

### Tutorial

Unity Editor Version: 2022.3.10f1

This tutorial will currently assume you're familiar with Supabase and have a project with various tables and authentication already configured; but you're wanting to add Unity mobile support.

1. Add your supabase anon key and url to the `SupabaseSettings.asset` file via Unity

2. Update your Supabase auth configuration to accept a "deeplink":

   a. https://supabase.com/dashboard/project/my-supabase-project-id/auth/url-configuration

   b. URL Configuration (Sidebar menu item)

   c. Redirect URLs (Subtitle)

   d. Add Url (Button)

3. Your newly added callback deeplink should look something like this (you can chose whatever name you desire)

4. Update `SupabaseActions.cs` to reflect your database and data model

![](/Assets/Tutorial/callback.png)

5. Build your project for iOS

   Note: each time the project build is regenerated or fully rebuilt, you'll need to update the plist again. To fix this a post build script can be put in place`

6. Open up the newly built projects folder and navigate to `Info.plist`

7. Add the following somewhere in the file (ensure that it doesn't override any other settings)

- This will allow for "deeplinking" which will be used to navigate back to your app from the web browser i.e: characterquest://callback

```
<key>CFBundleURLTypes</key>
    <array>
    <dict>
        <key>CFBundleTypeRole</key>
        <string>Editor</string>
        <key>CFBundleURLSchemes</key>
        <array>
        <string>characterquest</string>
        </array>
    </dict>
</array>
```

8. Deploy via Xcode to your device

9. Sign in with your provider of choice

10. Perform any authenticated action you've configured within `SupabaseActions.cs`
