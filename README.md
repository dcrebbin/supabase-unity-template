# Supabase Unity Template for iOS & Android (Social Sign-in)

Sign in via one of the providers (more is configurable)

![](/Assets/Tutorial/social-sign-in.png)

Once signed in via a web client you'll be redirected back to your app via a deeplink.

You can now perform authenticated supabase client requests.

![](/Assets/Tutorial/signed-in.png)

Forked from: https://github.com/wiverson/supabase-unity-template

Integrating Supabase into Unity featuring Social Sign-in and client-side supabase updates.

Unity Editor Version: 2022.3.10f1

This tutorial will currently assume you're familar with Supabase and have a project with various tables and authentication already configured; but you're wanting to add Unity mobile support.

### Steps

1. Add your supabase

1. Update your Supabase auth configuration to accept a "deeplink":

   - https://supabase.com/dashboard/project/my-supabase-project-id/auth/url-configuration
   - URL Configuration (Sidebar menu item)
   - Redirect URLs (Subtitle)
   - Add Url (Button)

1. Your newly added callback deeplink should now be added (you can chose whatever name you desire)

![](/Assets/Tutorial/callback.png)

3. Build your project for iOS

4. Open up the newly built projects folder and navigate to `Info.plist`

5. Add the following somewhere in the file (ensure that it doesn't override any other settings)
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
