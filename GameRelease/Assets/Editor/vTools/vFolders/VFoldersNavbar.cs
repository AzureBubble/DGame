#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor.IMGUI.Controls;
using Type = System.Type;
using static VFolders.Libs.VUtils;
using static VFolders.Libs.VGUI;
// using static VTools.VDebug;
using static VFolders.VFoldersData;
using static VFolders.VFolders;



namespace VFolders
{
    public class VFoldersNavbar
    {

        public void OnGUI(Rect navbarRect)
        {
            void updateState()
            {
                if (!curEvent.isLayout) return;


                openedFolderPath = window.GetMemberValue("m_SearchFilter")?.GetMemberValue<string[]>("folders")?.FirstOrDefault() ?? "Assets";

                isOneColumn = window.GetMemberValue<int>("m_ViewMode") == 0;

                isSearchActive = window.GetMemberValue<bool>("m_FocusSearchField")
                             || !window.GetMemberValue<string>("m_SearchFieldText").IsNullOrEmpty()
                             || GUI.GetNameOfFocusedControl() == "navbar search field";

            }

            void background()
            {
                var backgroundColor = Greyscale(isDarkTheme ? .235f : .8f);
                var lineColor = Greyscale(isDarkTheme ? .13f : .58f);

                navbarRect.Draw(backgroundColor);

                navbarRect.SetHeightFromBottom(1).MoveY(1).Draw(lineColor);

            }
            void hiddenMenu()
            {
                if (!curEvent.holdingAlt) return;
                if (!curEvent.isMouseUp) return;
                if (curEvent.mouseButton != 1) return;
                if (!navbarRect.IsHovered()) return;


                void selectData()
                {
                    Selection.activeObject = data;
                }
                void selectPalette()
                {
                    Selection.activeObject = palette;
                }
                void clearCache()
                {
                    VFoldersCache.instance.iconTextures_byKey.Clear();
                    VFoldersCache.instance.iconTextureDatas_byKey.Clear();
                    VFoldersCache.instance.folderStates_byGuid.Clear();

                    VFolders.folderInfoCache.Clear();

                    bookmarkWidthsCache.Clear();

                }



                GenericMenu menu = new();

                menu.AddDisabledItem(new GUIContent("vFolders hidden menu"));

                if (isOneColumn)
                {
                    var backForwardButtonsEnabled = EditorPrefsCached.GetBool("vFolders-showBackForwardButtonsInOneColumn", defaultValue: false);

                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Show + button"), false, backForwardButtonsEnabled ? () => EditorPrefsCached.SetBool("vFolders-showBackForwardButtonsInOneColumn", false) : null);
                    menu.AddItem(new GUIContent("Show < > buttons"), false, backForwardButtonsEnabled ? null : () => EditorPrefsCached.SetBool("vFolders-showBackForwardButtonsInOneColumn", true));
                }


                if (isTwoColumns)
                {
                    var forceCompactMode = EditorPrefsCached.GetBool("vFolders-forceCompactMode", defaultValue: false);

                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Enable compact bookmarks"), forceCompactMode, () => EditorPrefsCached.SetBool("vFolders-forceCompactMode", !forceCompactMode));
                }



                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Select data"), false, selectData);
                menu.AddItem(new GUIContent("Select palette"), false, selectPalette);

                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Clear cache"), false, clearCache);

                menu.ShowAsContext();

            }

            void backButton()
            {
                if (isOneColumn && !EditorPrefsCached.GetBool("vFolders-showBackForwardButtonsInOneColumn", defaultValue: false)) return;


                var buttonRect = navbarRect.SetWidth(30).MoveX(4);

                if (Application.unityVersion.StartsWith("6000"))
                    buttonRect = buttonRect.MoveY(-.49f);


                var iconName = "Chevron Left";
                var iconSize = 14;
                var colorNormal = Greyscale(isDarkTheme ? .75f : .2f);
                var colorHovered = Greyscale(isDarkTheme ? 1f : .2f);
                var colorPressed = Greyscale(isDarkTheme ? .75f : .5f);
                var colorDisabled = Greyscale(isDarkTheme ? .53f : .55f);


                var disabled = isOneColumn ? !history.prevTreeStates.Any() : !history.prevFolderPaths.Any();

                if (disabled) { IconButton(buttonRect, iconName, iconSize, colorDisabled, colorDisabled, colorDisabled); return; }


                if (!IconButton(buttonRect, iconName, iconSize, colorNormal, colorHovered, colorPressed)) return;

                if (isOneColumn)
                    history.MoveBack_OneColumn();
                else
                    history.MoveBack_TwoColumns();

            }
            void forwardButton()
            {
                if (isOneColumn && !EditorPrefsCached.GetBool("vFolders-showBackForwardButtonsInOneColumn", defaultValue: false)) return;


                var buttonRect = navbarRect.SetWidth(30).MoveX(30).MoveX(1).AddWidthFromMid(-6);

                if (Application.unityVersion.StartsWith("6000"))
                    buttonRect = buttonRect.MoveY(-.49f);


                var iconName = "Chevron Right";
                var iconSize = 14;
                var colorNormal = Greyscale(isDarkTheme ? .75f : .2f);
                var colorHovered = Greyscale(isDarkTheme ? 1f : .2f);
                var colorPressed = Greyscale(isDarkTheme ? .75f : .5f);
                var colorDisabled = Greyscale(isDarkTheme ? .53f : .55f);


                var disabled = isOneColumn ? !history.nextTreeStates.Any() : !history.nextFolderPaths.Any();

                if (disabled) { IconButton(buttonRect, iconName, iconSize, colorDisabled, colorDisabled, colorDisabled); return; }


                if (!IconButton(buttonRect, iconName, iconSize, colorNormal, colorHovered, colorPressed)) return;

                if (isOneColumn)
                    history.MoveForward_OneColumn();
                else
                    history.MoveForward_TwoColumns();

            }
            void plusButton_oneColumn()
            {
                if (!isOneColumn) return;
                if (EditorPrefsCached.GetBool("vFolders-showBackForwardButtonsInOneColumn", defaultValue: false)) return;


                var buttonRect = navbarRect.SetWidth(28).MoveX(4.5f);

                if (Application.unityVersion.StartsWith("6000"))
                    buttonRect = buttonRect.MoveY(-.49f);


                var iconName = "Plus Thicker";
                var iconSize = 16;
                var colorNormal = Greyscale(isDarkTheme ? .7f : .44f);
                var colorHovered = Greyscale(isDarkTheme ? 1f : .42f);
                var colorPressed = Greyscale(isDarkTheme ? .75f : .6f);


                if (!IconButton(buttonRect, iconName, iconSize, colorNormal, colorHovered, colorPressed)) return;

                EditorUtility.DisplayPopupMenu(buttonRect.MoveX(24), "Assets/Create", null);

            }


            void searchButton()
            {
                if (searchAnimationT == 1) return;


                var buttonRect = navbarRect.SetWidthFromRight(28).MoveX(-5);

                var iconName = "Search_";
                var iconSize = 16;
                var colorNormal = Greyscale(isDarkTheme ? .75f : .2f);
                var colorHovered = Greyscale(isDarkTheme ? 1f : .2f);
                var colorPressed = Greyscale(isDarkTheme ? .75f : .5f);


                if (!IconButton(buttonRect, iconName, iconSize, colorNormal, colorHovered, colorPressed)) return;

                window.SetMemberValue("m_FocusSearchField", true);

            }
            void plusButton_twoColumns()
            {
                if (isOneColumn) return;
                if (searchAnimationT == 1) return;


                var buttonRect = navbarRect.SetWidthFromRight(28).MoveX(-33);

                var iconName = "Plus";
                var iconSize = 16;
                var colorNormal = Greyscale(isDarkTheme ? .735f : .44f);
                var colorHovered = Greyscale(isDarkTheme ? 1f : .42f);
                var colorPressed = Greyscale(isDarkTheme ? .75f : .6f);


                if (!IconButton(buttonRect, iconName, iconSize, colorNormal, colorHovered, colorPressed)) return;

                if (curEvent.holdingAlt)
                {
                    history.prevTreeStates.Clear();
                    history.nextTreeStates.Clear();

                    history.prevFolderPaths.Clear();
                    history.nextFolderPaths.Clear();

                    history.lastScrollTime = 0;


                    window.Repaint();

                    return;


                    // for debug

                }

                EditorUtility.DisplayPopupMenu(buttonRect.MoveX(24), "Assets/Create", null);

            }
            void collapseAllButton_oneColumn()
            {
                if (!isOneColumn) return;
                if (searchAnimationT == 1) return;


                var buttonRect = navbarRect.SetWidthFromRight(28).MoveX(-33);

                var iconName = "Collapse";
                var iconSize = 16;
                var colorNormal = Greyscale(isDarkTheme ? .71f : .44f);
                var colorHovered = Greyscale(isDarkTheme ? 1f : .42f);
                var colorPressed = Greyscale(isDarkTheme ? .75f : .6f);


                if (!IconButton(buttonRect, iconName, iconSize, colorNormal, colorHovered, colorPressed)) return;

                controller.CollapseAll();

            }
            void bookmarks()
            {
                if (searchAnimationT == 1) return;
                if (isSearchActive && !curEvent.isRepaint) return;

                void createData()
                {
                    if (data) return;
                    if (!navbarRect.IsHovered()) return;
                    if (!DragAndDrop.objectReferences.Any()) return;

                    data = ScriptableObject.CreateInstance<VFoldersData>();

                    AssetDatabase.CreateAsset(data, GetScriptPath("VFolders").GetParentPath().CombinePath("vFolders Data.asset"));

                }
                void handleUndoRedo()
                {
                    if (!data) return;
                    if (curEvent.commandName != "UndoRedoPerformed") return;


                    if (repaintNeededAfterUndoRedo)
                        window.Repaint();

                    repaintNeededAfterUndoRedo = false;


                    bookmarkWidthsCache.Clear();
                    bookmarkCenterXCache.Clear();

                }
                void divider()
                {
                    if (!data) return;
                    if (!data.bookmarks.Any()) return;


                    var dividerRect = navbarRect.SetWidthFromRight(1).SetHeightFromMid(16).MoveX(-65).MoveX(isCompactMode ? 1.5f : .5f);

                    var dividerColor = Greyscale(isDarkTheme ? .33f : .64f);


                    dividerRect.Draw(dividerColor);

                }
                void gui()
                {
                    if (!data) return;

                    this.navbarRect = navbarRect;
                    this.bookmarksRect = navbarRect.AddWidth(-69).AddWidthFromRight(-60).MoveX(2).MoveX(isCompactMode ? -3 : 0);

                    BookmarksGUI();

                }

                createData();
                handleUndoRedo();
                divider();
                gui();

            }

            void searchField()
            {
                if (searchAnimationT == 0) return;

                var searchFieldRect = navbarRect.SetHeightFromMid(20).AddWidth(-33).SetWidthFromRight(240f.Min(window.position.width - (isOneColumn ? 195 : 223))).Move(-1, 2);


                GUILayout.BeginArea(searchFieldRect);
                GUILayout.BeginHorizontal();


                GUI.SetNextControlName("navbar search field");

                Space(2);
                window.InvokeMethod("SearchField");


                GUILayout.EndHorizontal();
                GUILayout.EndArea();

            }
            void searchOptions()
            {
                if (searchAnimationT == 0) return;

                var searchFieldRect = navbarRect.SetHeightFromMid(20).AddWidth(-33).SetWidthFromRight(240f.Min(window.position.width - (isOneColumn ? 195 : 223))).Move(-1, 2);
                var searchOptionsRect = navbarRect.SetHeightFromMid(20).SetXMax(searchFieldRect.x).SetWidthFromRight(123);




                GUILayout.BeginArea(searchOptionsRect);
                GUILayout.BeginHorizontal();




                GUILayout.FlexibleSpace();

                Space(3);
                window.InvokeMethod("TypeDropDown");

                var masksStartX = lastRect.x;


                window.InvokeMethod("AssetLabelsDropDown");

                if (!isOneColumn)
                    window.InvokeMethod("ButtonSaveFilter");


                window.InvokeMethod("ToggleHiddenPackagesVisibility");

                Space(4);





                var maskRect = searchOptionsRect.SetWidth(1).SetX(masksStartX - 1).MoveY(-3);
                var maskColor = Greyscale(isDarkTheme ? .235f : .8f);

                for (int i = 0; i < (isOneColumn ? 3 : 4); i++)
                {
                    maskRect.Draw(maskColor);
                    maskRect = maskRect.MoveX(26);
                }




                GUILayout.EndHorizontal();
                GUILayout.EndArea();


            }
            void closeSearchButton()
            {
                if (searchAnimationT == 0) return;


                var buttonRect = navbarRect.SetWidthFromRight(30).MoveX(-4);

                var iconName = "Cross";
                var iconSize = 16;
                var colorNormal = Greyscale(isDarkTheme ? .72f : .2f);
                var colorHovered = Greyscale(isDarkTheme ? .9f : .2f);
                var colorPressed = Greyscale(isDarkTheme ? .75f : .5f);


                if (!IconButton(buttonRect, iconName, iconSize, colorNormal, colorHovered, colorPressed)) return;

                window.SetMemberValue("m_SearchFieldText", "");
                window.GetMemberValue("m_SearchFilter").SetMemberValue("m_NameFilter", "");

                window.InvokeMethod("UpdateSearchDelayed");

                GUIUtility.keyboardControl = 0;

            }
            void closeSearchOnEsc()
            {
                if (!isSearchActive) return;
                if (curEvent.keyCode != KeyCode.Escape) return;

                window.SetMemberValue("m_SearchFieldText", "");
                window.GetMemberValue("m_SearchFilter").SetMemberValue("m_NameFilter", "");

                window.InvokeMethod("UpdateSearchDelayed");

                GUIUtility.keyboardControl = 0;

            }


            void searchAnimation()
            {
                if (!curEvent.isLayout) return;


                var lerpSpeed = 8f;

                if (isSearchActive)
                    MathUtil.SmoothDamp(ref searchAnimationT, 1, lerpSpeed, ref searchAnimationDerivative, editorDeltaTime);
                else
                    MathUtil.SmoothDamp(ref searchAnimationT, 0, lerpSpeed, ref searchAnimationDerivative, editorDeltaTime);


                if (isSearchActive && searchAnimationT > .99f)
                    searchAnimationT = 1;

                if (!isSearchActive && searchAnimationT < .01f)
                    searchAnimationT = 0;


                animatingSearch = searchAnimationT != 0 && searchAnimationT != 1;

            }

            void buttonsAndBookmarks()
            {
                SetGUIColor(Greyscale(1, (1 - searchAnimationT).Pow(2)));
                GUI.BeginGroup(window.position.SetPos(0, 0).MoveX(-searchAnimationDistance * searchAnimationT));

                searchButton();
                plusButton_twoColumns();
                collapseAllButton_oneColumn();
                bookmarks();

                GUI.EndGroup();
                ResetGUIColor();

            }
            void search()
            {
                SetGUIColor(Greyscale(1, searchAnimationT.Pow(2)));
                GUI.BeginGroup(window.position.SetPos(0, 0).MoveX(searchAnimationDistance * (1 - searchAnimationT)));

                searchField();
                searchOptions();
                closeSearchButton();
                closeSearchOnEsc();

                GUI.EndGroup();
                ResetGUIColor();

            }



            updateState();

            background();
            hiddenMenu();

            backButton();
            forwardButton();
            plusButton_oneColumn();

            searchAnimation();
            buttonsAndBookmarks();
            search();



            if (draggingBookmark || animatingDroppedBookmark || animatingGaps || animatingTooltip || animatingSearch)
                window.Repaint();

        }

        bool animatingSearch;

        float searchAnimationDistance = 90;
        float searchAnimationT;
        float searchAnimationDerivative;

        string openedFolderPath;

        bool isOneColumn;
        bool isTwoColumns => !isOneColumn;
        bool isSearchActive;

        bool isCompactMode
        {
            get
            {
                return isOneColumn || EditorPrefsCached.GetBool("vFolders-forceCompactMode", defaultValue: false);
            }
        }

        Rect navbarRect;
        Rect bookmarksRect;












        public void BookmarksGUI()
        {
            void bookmark(Vector2 centerPosition, Bookmark bookmark)
            {
                if (bookmark == null) return;
                if (!curEvent.isRepaint && !curEvent.isMouseUp) return;


                var bookmarkRect = Rect.zero.SetSize(GetBookmarkWidth(bookmark), bookmarksRect.height).SetMidPos(centerPosition);

                var IsHovered = bookmarkRect.IsHovered();
                var isSelected = openedFolderPath == bookmark.guid.ToPath() && !isCompactMode;
                var isPressed = bookmark == pressedBookmark;
                var isDragged = draggingBookmark && draggedBookmark == bookmark;


                void shadow()
                {
                    if (!draggingBookmark) return;
                    if (draggedBookmark != bookmark) return;

                    bookmarkRect.SetSizeFromMid(GetBookmarkWidth(bookmark) - 4, bookmarkRect.height - 4).DrawBlurred(Greyscale(0, .3f), 15);

                }
                void background()
                {
                    if (isCompactMode) return;
                    if (!isSelected && !IsHovered && !isDragged) return;

                    if (IsHovered && draggingBookmark && draggedBookmark != bookmark) return;


                    var backgroundRect = bookmarkRect.SetSizeFromMid(bookmarkRect.width - 7, bookmarkRect.height - 8).AddWidthFromRight(1);


                    var backgroundColor = isSelected ? Greyscale(isDarkTheme ? .35f : .7f)
                                                     : Greyscale(isDarkTheme ? .31f : .75f);
                    var outlineColor = isSelected ? Greyscale(isDarkTheme ? .21f : .65f)
                                                  : Greyscale(isDarkTheme ? .21f : .65f, .5f);


                    backgroundRect.Resize(-1).DrawRounded(outlineColor, 6);
                    backgroundRect.DrawRounded(backgroundColor, 5);

                }
                void backgroundCompact()
                {
                    if (!isCompactMode) return;

                    if (!IsHovered) return;
                    if (draggingBookmark && draggedBookmark != bookmark) return;


                    var backgroundColor = Greyscale(isDarkTheme ? .35f : .7f);

                    var backgroundRect = bookmarkRect.SetSizeFromMid(bookmarkRect.width - 2, bookmarkRect.width - 4);

                    backgroundRect.DrawRounded(backgroundColor, 4);

                }
                void icon()
                {
                    var folderInfo = VFolders.GetFolderInfo(bookmark.guid);

                    Texture iconTexture = folderInfo.hasIcon || folderInfo.hasColor ? VFolders.GetSmallFolderIcon(folderInfo)
                                                                                    : EditorIcons.GetIcon("Folder Icon");

                    var iconRect = isCompactMode ? bookmarkRect.SetSizeFromMid(iconSize) : bookmarkRect.SetWidth(iconSize).SetHeightFromMid(iconSize).MoveX(bookmarkSpacing / 2);

                    iconRect = iconRect.SetWidthFromMid(iconRect.height * iconTexture.width / iconTexture.height);



                    var opacity = folderInfo.hasColor ? .96f : .92f;

                    if (isSelected)
                        opacity = 1;

                    if (isPressed && !isDragged)
                        opacity = .73f;

                    if (bookmark.isDeleted)
                        opacity = .4f;



                    SetGUIColor(Greyscale(1, opacity));

                    GUI.DrawTexture(iconRect, iconTexture);

                    ResetGUIColor();

                }
                void name()
                {
                    if (isCompactMode) return;
                    if (!curEvent.isRepaint) return;


                    var nameRect = bookmarkRect.MoveX(bookmarkSpacing / 2 + iconSize + iconSpacing);

                    var makeExtraBright = isSelected && isDarkTheme;


                    if (!makeExtraBright)
                    {
                        var opacity = .95f;

                        if (isSelected)
                            opacity = 1;

                        if (isPressed && !isDragged)
                            opacity = .82f;

                        if (bookmark.isDeleted)
                            opacity = .4f;



                        SetGUIColor(Greyscale(1, opacity));

                        GUI.Label(nameRect, bookmark.name);

                        ResetGUIColor();

                    }

                    if (makeExtraBright)
                    {
                        var opacity = .85f;

                        nameRect = nameRect.SetHeightFromMid(16).AddHeight(4);


                        SetGUIColor(Greyscale(1, opacity));

                        GUI.skin.GetStyle("WhiteLabel").Draw(nameRect, bookmark.name, false, false, false, false);

                        ResetGUIColor();

                    }

                }
                void tooltip()
                {
                    if (!isCompactMode) return;

                    if (bookmark != (draggingBookmark ? (draggedBookmark) : (lastHoveredBookmark))) return;
                    if (tooltipOpacity == 0) return;

                    var fontSize = 11; // ,maybe 12
                    var tooltipText = bookmark.isDeleted ? "Deleted" : bookmark.name;

                    Rect tooltipRect;

                    void set_tooltipRect()
                    {
                        var width = tooltipText.GetLabelWidth(fontSize) + 6;
                        var height = 16 + (fontSize - 12) * 2;

                        var yOffset = 28;
                        var rightMargin = -1;


                        tooltipRect = Rect.zero.SetMidPos(centerPosition.x, centerPosition.y + yOffset).SetSizeFromMid(width, height);


                        var maxXMax = navbarRect.xMax - rightMargin;

                        if (tooltipRect.xMax > maxXMax)
                            tooltipRect = tooltipRect.MoveX(maxXMax - tooltipRect.xMax);

                    }
                    void shadow()
                    {
                        var shadowAmount = .33f;
                        var shadowRadius = 10;

                        tooltipRect.DrawBlurred(Greyscale(0, shadowAmount).MultiplyAlpha(tooltipOpacity), shadowRadius);

                    }
                    void background()
                    {
                        var cornerRadius = 5;

                        var backgroundColor = Greyscale(isDarkTheme ? .13f : .9f);
                        var outerEdgeColor = Greyscale(isDarkTheme ? .25f : .6f);
                        var innerEdgeColor = Greyscale(isDarkTheme ? .0f : .95f);

                        tooltipRect.Resize(-1).DrawRounded(outerEdgeColor.SetAlpha(tooltipOpacity.Pow(2)), cornerRadius + 1);
                        tooltipRect.Resize(0).DrawRounded(innerEdgeColor.SetAlpha(tooltipOpacity.Pow(2)), cornerRadius + 0);
                        tooltipRect.Resize(1).DrawRounded(backgroundColor.SetAlpha(tooltipOpacity), cornerRadius - 1);

                    }
                    void text()
                    {
                        var textRect = tooltipRect.MoveY(-.5f);

                        var textColor = Greyscale(1f);

                        SetLabelAlignmentCenter();
                        SetLabelFontSize(fontSize);
                        SetGUIColor(textColor.SetAlpha(tooltipOpacity));

                        GUI.Label(textRect, tooltipText);

                        ResetLabelStyle();
                        ResetGUIColor();

                    }

                    set_tooltipRect();
                    shadow();
                    background();
                    text();

                }
                void click()
                {
                    if (!IsHovered) return;
                    if (!curEvent.isMouseUp) return;


                    curEvent.Use();

                    if (draggingBookmark) return;
                    if ((curEvent.mousePosition - mouseDownPosiion).magnitude > 2) return;
                    if (bookmark.isDeleted) return;


                    if (isOneColumn)
                        controller.RevealFolder(bookmark.guid.ToPath(), expand: true, highlight: true, snapToTopMargin: true);

                    if (isTwoColumns)
                    {
                        controller.RevealFolder(bookmark.guid.ToPath(), expand: false, highlight: false, snapToTopMargin: false);
                        controller.OpenFolder(bookmark.guid.ToPath());
                    }


                    lastClickedBookmark = bookmark;

                    hideTooltip = true;

                }


                bookmarkRect.MarkInteractive();

                shadow();
                background();
                backgroundCompact();
                icon();
                name();
                tooltip();
                click();

            }

            void normalBookmark(int i)
            {
                if (data.bookmarks[i] == droppedBookmark && animatingDroppedBookmark) return;

                var centerX = GetBookmarkCenterX(i);
                var centerY = bookmarksRect.height / 2;


                var minX = centerX - GetBookmarkWidth(data.bookmarks[i]) / 2;

                if (minX < bookmarksRect.x) return;

                lastBookmarkX = minX;


                bookmark(new Vector2(centerX, centerY), data.bookmarks[i]);

            }
            void normalBookmarks()
            {
                for (int i = 0; i < data.bookmarks.Count; i++)
                    normalBookmark(i);

            }
            void draggedBookmark_()
            {
                if (!draggingBookmark) return;

                var centerX = curEvent.mousePosition.x + draggedBookmarkHoldOffset.x;
                var centerY = bookmarksRect.IsHovered() ? bookmarksRect.height / 2 : curEvent.mousePosition.y;

                bookmark(new Vector2(centerX, centerY), draggedBookmark);

            }
            void droppedBookmark_()
            {
                if (!animatingDroppedBookmark) return;

                var centerX = droppedBookmarkX;
                var centerY = bookmarksRect.height / 2;

                bookmark(new Vector2(centerX, centerY), droppedBookmark);

            }


            ClearCacheIfNeeded();

            BookmarksMouseState();
            BookmarksDragging();
            BookmarksAnimations();

            normalBookmarks();
            draggedBookmark_();
            droppedBookmark_();

        }

        float iconSize = 16;
        float iconSpacing = 1;
        float bookmarkSpacing = 16;

        float bookmarkWidth_compactMode = 24;

        bool repaintNeededAfterUndoRedo;

        public float lastBookmarkX;




        int GetBookmarkIndex(float mouseX)
        {
            var curBookmarkWidthSum = 0f;

            for (int i = 0; i < data.bookmarks.Count; i++)
            {
                curBookmarkWidthSum += GetBookmarkWidth(data.bookmarks[i]);

                if (bookmarksRect.xMax - curBookmarkWidthSum < mouseX + .5f)
                    return i;
            }

            return data.bookmarks.Count;

        }

        float GetBookmarkWidth(Bookmark bookmark)
        {
            if (!animatingBookmarks)
                if (bookmarkWidthsCache.TryGetValue(bookmark, out var cachedWidth)) return cachedWidth;



            var width = isCompactMode ? bookmarkWidth_compactMode
                                      : bookmark.name.GetLabelWidth() + iconSize + iconSpacing + bookmarkSpacing;


            if (!animatingBookmarks)
                bookmarkWidthsCache[bookmark] = width;
            else
                bookmarkWidthsCache.Clear();


            return width;

        }
        float GetBookmarkCenterX(int i, bool includeGaps = true)
        {
            if (!animatingBookmarks)
                if (bookmarkCenterXCache.TryGetValue(i, out var cachedCenterX)) return cachedCenterX;



            var centerX = bookmarksRect.xMax
                        - GetBookmarkWidth(data.bookmarks[i.Clamp(0, data.bookmarks.Count - 1)]) / 2
                        - data.bookmarks.Take(i).Sum(r => GetBookmarkWidth(r))
                        - (includeGaps ? gaps.Take(i + 1).Sum() : 0);


            if (!animatingBookmarks)
                bookmarkCenterXCache[i] = centerX;
            else
                bookmarkCenterXCache.Clear();


            return centerX;

        }

        Dictionary<Bookmark, float> bookmarkWidthsCache = new();
        Dictionary<int, float> bookmarkCenterXCache = new();




        void ClearCacheIfNeeded()
        {
            var modeChanged = wasCompactMode != isCompactMode;
            var windowResized = prevWindowWidth != window.position.width;
            var undoPerformed = curEvent.commandName == "UndoRedoPerformed";
            var renameHappened = curEvent.commandName == "NewKeyboardFocus";

            wasCompactMode = isCompactMode;
            prevWindowWidth = window.position.width;



            if (!modeChanged && !windowResized && !undoPerformed && !renameHappened) return;

            bookmarkWidthsCache.Clear();
            bookmarkCenterXCache.Clear();

        }

        bool wasCompactMode;
        float prevWindowWidth;












        void BookmarksMouseState()
        {
            void down()
            {
                if (!curEvent.isMouseDown) return;
                if (!bookmarksRect.IsHovered()) return;

                mousePressed = true;

                mouseDownPosiion = curEvent.mousePosition;

                var pressedBookmarkIndex = GetBookmarkIndex(mouseDownPosiion.x);

                if (pressedBookmarkIndex.IsInRangeOf(data.bookmarks))
                    pressedBookmark = data.bookmarks[pressedBookmarkIndex];

                doubleclickUnhandled = curEvent.clickCount == 2;

                curEvent.Use();

            }
            void up()
            {
                if (!curEvent.isMouseUp) return;

                mousePressed = false;
                pressedBookmark = null;

            }
            void hover()
            {
                var hoveredBookmarkIndex = GetBookmarkIndex(curEvent.mousePosition.x);

                mouseHoversBookmark = bookmarksRect.IsHovered() && hoveredBookmarkIndex.IsInRangeOf(data.bookmarks);

                if (mouseHoversBookmark)
                    lastHoveredBookmark = data.bookmarks[hoveredBookmarkIndex];


            }

            down();
            up();
            hover();

        }

        bool mouseHoversBookmark;
        bool mousePressed;
        bool doubleclickUnhandled;

        Vector2 mouseDownPosiion;

        Bookmark pressedBookmark;
        Bookmark lastHoveredBookmark;






        void BookmarksDragging()
        {
            void initFromOutside()
            {
                if (draggingBookmark) return;
                if (!bookmarksRect.IsHovered()) return;
                if (!curEvent.isDragUpdate) return;
                if (DragAndDrop.objectReferences.FirstOrDefault() is not Object draggedObject) return;
                if (draggedObject is not DefaultAsset) return;

                animatingDroppedBookmark = false;

                draggingBookmark = true;
                draggingBookmarkFromInside = false;

                draggedBookmark = new Bookmark(draggedObject);
                draggedBookmarkHoldOffset = Vector2.zero;

            }
            void initFromInside()
            {
                if (draggingBookmark) return;
                if (!mousePressed) return;
                if ((curEvent.mousePosition - mouseDownPosiion).magnitude <= 2) return;
                if (pressedBookmark == null) return;

                var i = GetBookmarkIndex(mouseDownPosiion.x);

                if (i >= data.bookmarks.Count) return;
                if (i < 0) return;


                animatingDroppedBookmark = false;

                draggingBookmark = true;
                draggingBookmarkFromInside = true;

                draggedBookmark = data.bookmarks[i];
                draggedBookmarkHoldOffset = new Vector2(GetBookmarkCenterX(i) - mouseDownPosiion.x, bookmarksRect.center.y - mouseDownPosiion.y);

                gaps[i] = GetBookmarkWidth(draggedBookmark);


                data.RecordUndo();

                data.bookmarks.Remove(draggedBookmark);

            }

            void acceptFromOutside()
            {
                if (!draggingBookmark) return;
                if (!curEvent.isDragPerform) return;
                if (!bookmarksRect.IsHovered()) return;

                DragAndDrop.AcceptDrag();
                curEvent.Use();

                data.RecordUndo();

                accept();

                data.Dirty();

            }
            void acceptFromInside()
            {
                if (!draggingBookmark) return;
                if (!curEvent.isMouseUp) return;
                if (!bookmarksRect.IsHovered()) return;

                curEvent.Use();
                EditorGUIUtility.hotControl = 0;

                DragAndDrop.PrepareStartDrag(); // fixes phantom dragged component indicator after reordering bookmarks

                data.RecordUndo();
                data.Dirty();

                accept();

            }
            void accept()
            {
                draggingBookmark = false;
                draggingBookmarkFromInside = false;
                mousePressed = false;

                data.bookmarks.AddAt(draggedBookmark, insertDraggedBookmarkAtIndex);

                gaps[insertDraggedBookmarkAtIndex] -= GetBookmarkWidth(draggedBookmark);
                gaps.AddAt(0, insertDraggedBookmarkAtIndex);

                droppedBookmark = draggedBookmark;

                droppedBookmarkX = curEvent.mousePosition.x + draggedBookmarkHoldOffset.x;
                droppedBookmarkXDerivative = 0;
                animatingDroppedBookmark = true;

                draggedBookmark = null;

                EditorGUIUtility.hotControl = 0;

                repaintNeededAfterUndoRedo = true;

            }

            void cancelFromOutside()
            {
                if (!draggingBookmark) return;
                if (draggingBookmarkFromInside) return;
                if (bookmarksRect.IsHovered()) return;

                draggingBookmark = false;
                mousePressed = false;

            }
            void cancelFromInsideAndDelete()
            {
                if (!draggingBookmark) return;
                if (!curEvent.isMouseUp) return;
                if (bookmarksRect.IsHovered()) return;

                draggingBookmark = false;

                DragAndDrop.PrepareStartDrag(); // fixes phantom dragged component indicator after reordering bookmarks

                data.Dirty();

                repaintNeededAfterUndoRedo = true;

            }

            void update()
            {
                if (!draggingBookmark) return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

                if (draggingBookmarkFromInside) // otherwise it breaks vTabs dragndrop
                    EditorGUIUtility.hotControl = EditorGUIUtility.GetControlID(FocusType.Passive);



                insertDraggedBookmarkAtIndex = GetBookmarkIndex(curEvent.mousePosition.x + draggedBookmarkHoldOffset.x);

            }

            void dropIntoBookmark()
            {
                if (draggingBookmark) return;
                if (!bookmarksRect.IsHovered()) return;
                if (!DragAndDrop.objectReferences.Any()) return;
                if (!DragAndDrop.objectReferences.Any(r => r is not DefaultAsset)) return;
                if (!DragAndDrop.objectReferences.All(r => AssetDatabase.Contains(r))) return;

                if (!mouseHoversBookmark) return;
                if (lastHoveredBookmark.isDeleted) return;


                if (curEvent.isDragUpdate)
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;


                if (!curEvent.isDragPerform) return;

                DragAndDrop.AcceptDrag();

                curEvent.Use();


                var oldPaths = DragAndDrop.objectReferences.Select(r => r.GetPath()).ToList();

                var newPaths = oldPaths.Select(r => lastHoveredBookmark.guid.ToPath().CombinePath(r.GetFilename(withExtension: true)))
                                       .Select(r => AssetDatabase.GenerateUniqueAssetPath(r)).ToList();


                typeof(Undo).GetMethod("RegisterAssetsMoveUndo", maxBindingFlags).Invoke(null, new object[] { oldPaths.ToArray() });

                for (int i = 0; i < newPaths.Count; i++)
                    AssetDatabase.MoveAsset(oldPaths[i], newPaths[i]);

            }


            initFromOutside();
            initFromInside();

            acceptFromOutside();
            acceptFromInside();

            cancelFromOutside();
            cancelFromInsideAndDelete();

            update();

            dropIntoBookmark();

        }

        bool draggingBookmark;
        bool draggingBookmarkFromInside;

        int insertDraggedBookmarkAtIndex;

        Vector2 draggedBookmarkHoldOffset;

        Bookmark draggedBookmark;
        Bookmark droppedBookmark;






        void BookmarksAnimations()
        {
            if (!curEvent.isLayout) return;

            void gaps_()
            {
                var makeSpaceForDraggedBookmark = draggingBookmark && bookmarksRect.IsHovered();

                var lerpSpeed = 12;

                for (int i = 0; i < gaps.Count; i++)
                    if (makeSpaceForDraggedBookmark && i == insertDraggedBookmarkAtIndex)
                        gaps[i] = MathUtil.Lerp(gaps[i], GetBookmarkWidth(draggedBookmark), lerpSpeed, editorDeltaTime);
                    else
                        gaps[i] = MathUtil.Lerp(gaps[i], 0, lerpSpeed, editorDeltaTime);



                for (int i = 0; i < gaps.Count; i++)
                    if (gaps[i].Approx(0))
                        gaps[i] = 0;



                animatingGaps = gaps.Any(r => r > .1f);


            }
            void droppedBookmark_()
            {
                if (!animatingDroppedBookmark) return;

                var lerpSpeed = 8;

                var targX = GetBookmarkCenterX(data.bookmarks.IndexOf(droppedBookmark), includeGaps: false);

                MathUtil.SmoothDamp(ref droppedBookmarkX, targX, lerpSpeed, ref droppedBookmarkXDerivative, editorDeltaTime);

                if ((droppedBookmarkX - targX).Abs() < .5f)
                    animatingDroppedBookmark = false;

            }
            void tooltip()
            {
                if (!mouseHoversBookmark || lastHoveredBookmark != lastClickedBookmark)
                    hideTooltip = false;


                var lerpSpeed = UnityEditorInternal.InternalEditorUtility.isApplicationActive ? 15 : 12321;

                if (mouseHoversBookmark && !draggingBookmark && !hideTooltip)
                    MathUtil.SmoothDamp(ref tooltipOpacity, 1, lerpSpeed, ref tooltipOpacityDerivative, editorDeltaTime);
                else
                    MathUtil.SmoothDamp(ref tooltipOpacity, 0, lerpSpeed, ref tooltipOpacityDerivative, editorDeltaTime);


                if (tooltipOpacity > .99f)
                    tooltipOpacity = 1;

                if (tooltipOpacity < .01f)
                    tooltipOpacity = 0;


                animatingTooltip = tooltipOpacity != 0 && tooltipOpacity != 1;

            }

            gaps_();
            droppedBookmark_();
            tooltip();

        }

        float droppedBookmarkX;
        float droppedBookmarkXDerivative;

        float tooltipOpacity;
        float tooltipOpacityDerivative;

        bool animatingDroppedBookmark;
        bool animatingGaps;
        bool animatingTooltip;
        bool animatingBookmarks => animatingDroppedBookmark || animatingGaps;

        bool hideTooltip;

        List<float> gaps
        {
            get
            {
                while (_gaps.Count < data.bookmarks.Count + 1) _gaps.Add(0);
                while (_gaps.Count > data.bookmarks.Count + 1) _gaps.RemoveLast();

                return _gaps;

            }
        }
        List<float> _gaps = new();

        Bookmark lastClickedBookmark;













        public VFoldersNavbar(EditorWindow window)
        {
            this.window = window;

            if (!VFoldersHistorySingleton.instance.histories_byWindow.TryGetValue(window, out history))
                VFoldersHistorySingleton.instance.histories_byWindow[window] = history = new VFoldersHistory(window);

        }

        public EditorWindow window;
        public VFoldersHistory history;

        public VFoldersController controller => VFolders.controllers_byWindow[window];


    }
}
#endif