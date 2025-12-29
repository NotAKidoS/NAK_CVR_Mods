using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.UI;
using UnityEngine;

namespace NAK.DummyMenu;

public static class CreateDummyMenu
{
    public static void Create()
    {
        CreateDefaultExamplePageIfNeeded();
        
        GameObject cohtmlRootObject = GameObject.Find("Cohtml");
        
        // Create menu rig
        // Cohtml -> Root -> Offset -> Menu
        GameObject dummyMenuRoot = new("DummyMenuRoot");
        GameObject dummyMenuOffset = new("DummyMenuOffset");
        GameObject dummyMenuItself = new("DummyMenu");
        dummyMenuItself.transform.SetParent(dummyMenuOffset.transform);
        dummyMenuOffset.transform.SetParent(dummyMenuRoot.transform);
        dummyMenuRoot.transform.SetParent(cohtmlRootObject.transform);
        
        // Add dummy menu position helper
        DummyMenuPositionHelper positionHelper = dummyMenuRoot.AddComponent<DummyMenuPositionHelper>();
        positionHelper._offsetTransform = dummyMenuOffset.transform;
        positionHelper.menuTransform = dummyMenuItself.transform;
        
        // Add components to menu (MeshFilter, MeshRenderer, Animator, CohtmlControlledView)
        MeshFilter meshFilter = dummyMenuItself.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = dummyMenuItself.AddComponent<MeshRenderer>();
        Animator animator = dummyMenuItself.AddComponent<Animator>();
        CohtmlControlledView controlledView = dummyMenuItself.AddComponent<CohtmlControlledView>();
        
        // Add dummy menu manager
        DummyMenuManager menuManager = dummyMenuItself.AddComponent<DummyMenuManager>();
        menuManager.cohtmlView = controlledView;
        menuManager._viewAnimator = animator;
        menuManager._uiRenderer = meshRenderer;
        
        // Steal from main menu
        menuManager.pattern = ViewManager.Instance.pattern;
        menuManager.timing = ViewManager.Instance.timing;
        meshFilter.mesh = ViewManager.Instance.GetComponent<MeshFilter>().sharedMesh;
        meshRenderer.sharedMaterial = null; // assign empty material 
        animator.runtimeAnimatorController = ViewManager.Instance.GetComponent<Animator>().runtimeAnimatorController;
        
        // Put everything on UI Internal layer
        dummyMenuRoot.SetLayerRecursive(CVRLayers.UIInternal);
        
        // Apply initial settings
        menuManager.TrySetMenuPage();
        
        float pageWidth = ModSettings.EntryPageWidth.Value;
        float pageHeight = ModSettings.EntryPageHeight.Value;
        positionHelper.UpdateAspectRatio(pageWidth, pageHeight);
    }

    internal const string ExampleDummyMenuPath = "UIResources/DummyMenu/_example.html";
    internal static string GetFullCouiPath(string couiPath) => Path.Combine(Application.streamingAssetsPath, "Cohtml", couiPath);

    private static void CreateDefaultExamplePageIfNeeded()
    {
        // Check if there is a file at our default path
        string fullPath = GetFullCouiPath(ExampleDummyMenuPath);
        if (File.Exists(fullPath))
        {
            DummyMenuMod.Logger.Msg($"Dummy menu HTML file already exists at {fullPath}. No need to create a new one.");
            return;
        }
        DummyMenuMod.Logger.Msg($"No dummy menu HTML file found at {fullPath}. Creating a default one.");
        
        // Create the directory if it doesn't exist
        string directory = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory!);
        
        // Create a default HTML file
        using StreamWriter writer = new(fullPath, false);
        writer.Write(DefaultHtmlContent);
        writer.Flush();
        writer.Close();
        DummyMenuMod.Logger.Msg($"Created default dummy menu HTML file at {fullPath}. You can now open the dummy menu in-game.");
    }

    #region Default HTML Content

    private const string DefaultHtmlContent = """
                                              <!doctype html>
                                              <html lang="en">
                                              <head>
                                                  <meta charset="utf-8">
                                                  <meta http-equiv="X-UA-Compatible" content="IE=edge">
                                                  <title>Cohtml Bubble Pop — 1280×720</title>
                                                  <style>
                                                      html,body {
                                                          height:100%; margin:0;
                                                          background:#061018;
                                                          display:flex; align-items:center; justify-content:center;
                                                          font-family:Arial,Helvetica,sans-serif;
                                                      }
                                                      #stage {
                                                          width:1280px; height:720px;
                                                          position:relative; overflow:hidden;
                                                          border-radius:20px;
                                                          border:6px solid rgba(0,255,255,0.12);
                                                          box-shadow:0 20px 60px rgba(0,0,0,0.7), inset 0 0 80px rgba(0,255,255,0.03);
                                                          background:linear-gradient(135deg,#03121a 0%,#071826 60%);
                                                          -webkit-user-select:none; user-select:none;
                                                      }
                                                      .panel {
                                                          position:absolute; left:50%; top:50%;
                                                          transform:translate(-50%,-50%);
                                                          text-align:center; color:#dff9ff;
                                                          pointer-events:none;
                                                          z-index:10;
                                                      }
                                                      .title {
                                                          font-size:56px; font-weight:700; letter-spacing:1px;
                                                          margin:0; text-shadow:0 6px 30px rgba(0,255,255,0.06);
                                                      }
                                                      .subtitle { font-size:20px; opacity:0.85; margin-top:6px; }
                                                      /* bubbles */
                                                      .bubble {
                                                          position:absolute; border-radius:50%;
                                                          pointer-events:auto;
                                                          will-change:transform,opacity;
                                                          box-shadow:0 6px 40px rgba(0,255,255,0.12),
                                                          inset 0 -8px 30px rgba(255,255,255,0.02);
                                                          display:block; transform-origin:center center;
                                                      }
                                                      .pop-anim { animation:pop .28s ease forwards }
                                                      @keyframes pop {
                                                          0%{transform:scale(1)}
                                                          50%{transform:scale(1.25)}
                                                          100%{transform:scale(0);opacity:0}
                                                      }
                                                  </style>
                                              </head>
                                              <body>
                                              <div id="stage" role="application" aria-label="Bubble pop scene">
                                                  <div class="panel">
                                                      <h1 class="title">Hello World</h1>
                                                      <div class="subtitle">Bubble pop — Gameface fixed</div>
                                                  </div>
                                              </div>
                                              <script>
                                                  (function(){
                                                      const stage = document.getElementById('stage');
                                                      const bubbles = new Set();
                                                      let lastTime = performance.now();

                                                      function rand(min,max){ return Math.random()*(max-min)+min; }

                                                      function createBubbleAt(x){
                                                          const rect = stage.getBoundingClientRect();
                                                          const size = Math.round(rand(28,110));
                                                          const b = document.createElement('div');
                                                          b.className='bubble';
                                                          b.style.width = size + 'px';
                                                          b.style.height = size + 'px';
                                                          const left = Math.max(0, Math.min(rect.width - size, x - size/2));
                                                          b.dataset.x = left;
                                                          b.dataset.y = rect.height + size;
                                                          b.style.left = left + 'px';
                                                          b.style.top = rect.height + 'px';
                                                          b.style.background =
                                                              'radial-gradient(circle at 30% 25%, rgba(255,255,255,0.9), rgba(255,255,255,0.25) 10%, rgba(0,200,230,0.18) 40%, rgba(0,40,60,0.06) 100%)';
                                                          b.style.border = '1px solid rgba(255,255,255,0.08)';
                                                          b.style.opacity = '0';
                                                          b._vy = -rand(20,120);
                                                          b._vx = rand(-40,40);
                                                          b._rot = rand(-120,120);
                                                          b._life = 0;
                                                          b._size = size;
                                                          b._ttl = rand(4200,12000);
                                                          b._popped = false;

                                                          b.addEventListener('pointerdown', (e)=>{
                                                              e.stopPropagation();
                                                              popBubble(b);
                                                          });

                                                          stage.appendChild(b);
                                                          bubbles.add(b);
                                                          return b;
                                                      }

                                                      function popBubble(b){
                                                          if(!b || b._popped) return;
                                                          b._popped = true;
                                                          b.classList.add('pop-anim');
                                                          setTimeout(()=>{ try{ b.remove(); }catch{} bubbles.delete(b); },300);
                                                      }

                                                      let autoInterval = setInterval(()=>{
                                                          const rect = stage.getBoundingClientRect();
                                                          createBubbleAt(rand(40, rect.width - 40));
                                                      }, 420);

                                                      stage.addEventListener('pointerdown', (e)=>{
                                                          if(e.target.classList.contains('bubble')) return;
                                                          const rect = stage.getBoundingClientRect();
                                                          const x = e.clientX - rect.left;
                                                          createBubbleAt(x);
                                                      });

                                                      function animate(now){
                                                          const dt = Math.min(80, now - lastTime);
                                                          lastTime = now;
                                                          const rect = stage.getBoundingClientRect();
                                                          for(const b of Array.from(bubbles)){
                                                              if(b._popped) continue;
                                                              b._life += dt;
                                                              const nx = parseFloat(b.dataset.x) + b._vx * (dt/1000);
                                                              const ny = parseFloat(b.dataset.y) + b._vy * (dt/1000);
                                                              b.dataset.x = nx; b.dataset.y = ny;
                                                              const rot = (b._rot * (b._life/1000));
                                                              b.style.transform = `rotate(${rot}deg)`;
                                                              b.style.left = nx + 'px'; b.style.top = ny + 'px';
                                                              const ttl = b._ttl;
                                                              if(b._life > ttl){
                                                                  const over = (b._life - ttl)/1000;
                                                                  b.style.opacity = Math.max(0, 1 - over*2);
                                                              }else{
                                                                  b.style.opacity = Math.min(1, b._life / 200);
                                                              }
                                                              if(ny + b._size < -150 || parseFloat(b.style.opacity) <= 0.01){
                                                                  try{ b.remove(); }catch{} bubbles.delete(b);
                                                              }
                                                          }
                                                          requestAnimationFrame(animate);
                                                      }
                                                      requestAnimationFrame(animate);

                                                  })();
                                              </script>
                                              </body>
                                              </html>
                                              """;

    #endregion Default HTML Content
}