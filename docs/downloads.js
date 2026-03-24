const owner = "Fazin85";
    const repo = "betasharp";

    async function fetchReleases() {
      const url = `https://api.github.com/repos/${owner}/${repo}/releases`;
      const response = await fetch(url, {
        headers: {
          // This enables rendered HTML (body_html)
          Accept: "application/vnd.github+json"
        }
      });

      const releases = await response.json();

      const container = document.getElementById("releases");

      releases.forEach(release => {
        const div = document.createElement("div");
        div.className = "release";

        const title = document.createElement("h3");
        title.textContent = release.name || release.tag_name;

        // Optional visual badge as well
        if (release.prerelease) {
          const badge = document.createElement("span");
          badge.className = "badge";
          badge.textContent = "Pre-release";
          title.appendChild(badge);
        }

        const assetsDiv = document.createElement("div");
        assetsDiv.className = "assets";

        if (release.assets.length === 0) {
          const noAssets = document.createElement("p");
          noAssets.textContent = "No downloadable files.";
          assetsDiv.appendChild(noAssets);
        } else {
          release.assets.forEach(asset => {
            const link = document.createElement("a");
            link.href = asset.browser_download_url;
            link.textContent = asset.name;
            link.download = release.name + "-" + asset.name;
            assetsDiv.appendChild(link);
          });
        }

        div.appendChild(title);
        div.appendChild(assetsDiv);

        container.appendChild(div);
      });
    }

    fetchReleases();