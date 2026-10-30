# DazPackager

Automatically generates the `Manifest.dsx` and `Supplement.dsx` files needed to make a `.zip` installable via **Daz Install Manager (DIM)** — useful for any content purchased outside the Daz3D store (Renderosity, RenderHub, independent creators, etc.).

> [!TIP]
> This repo is forked to include a Github Action which builds the .exe with a self contained dotnet if you dont have it installed.
>
> You can download the pre-built .exe from releases
>
> You can also build it yourself by forking and running "Build DazPackager" action from Actions tab, the release will be automatically moved to your repo's Releases section.

# Usage

- Open powershell from the folder where .exe is located.
  
- Run:

    ```powershell
    .\DazPackager.exe "path_to_archive_or_folder"
    ```
    Replace `DazPackager.exe` with actual name of .exe if it's different.

- Move the generated archive to the folder watched by Daz Install Manager. <br>
*(you can configure it in `DIM > Advanced Settings > Downloads > Package Archive`)*

Daz Install Manager should be able to detect the product on refresh or next run and show it in 'Ready to Install' section.
  
# Notes

- For more details refer to full README in [Original Repo](https://github.com/raphaelbouvigne/DazPackager).

- If your input is archive make sure it is `.zip` (or simply extract the folder and use that) and not rar or anything else which will result in an error.
