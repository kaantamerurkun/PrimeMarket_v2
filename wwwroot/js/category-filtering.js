document.addEventListener("DOMContentLoaded", function () {
    const allCategories = {
        "Phone": ["IOS Phone", "Android Phone", "Other Phones", "Spare Parts", "Phone Accessories"],
        "Tablets": ["IOS Tablets", "Android Tablets", "Other Tablets", "Tablet Accessories"],
        "Electronics": {
            "Computer": ["Laptops", "Desktops", "Computer Accessories", "Computer Components", "Monitors"],
            "White Goods": ["Fridges", "Washers", "Dishwashers", "Stoves", "Ovens", "Microwave Ovens"],
            "Electrical Domestic Appliances": ["Vacuum Cleaner", "Beverage Preparation", "Food Preparation", "Iron", "Sewing Machine"],
            "Televisions": [],
            "Heating & Cooling": [],
            "Cameras": [],
            "Computer Accessories": ["Keyboards", "Speakers", "Headphones & Earphones", "Webcams", "Microphones", "Mouse", "Computer Bags"]
        }
    };

    // Store references to open menus for better control
    let openMainMenu = null;
    let openSubMenu = null;

    // Track the active filter for listings
    let activeFilter = {
        category: null,
        subcategory: null,
        detailCategory: null
    };

    function createDropdownItem(name, subItems, level = 'main') {
        const li = document.createElement("li");
        li.style.position = "relative";
        li.style.cursor = "pointer";
        li.textContent = name;
        li.dataset.value = name;
        li.dataset.level = level;

        if (subItems && Object.keys(subItems).length > 0) {
            const ul = document.createElement("ul");
            ul.className = "dropdown-menu";
            ul.style.position = "absolute";
            ul.style.top = "100%";
            ul.style.listStyle = "none";
            ul.style.left = "0";
            ul.style.backgroundColor = "#fff";
            ul.style.padding = "10px";
            ul.style.border = "1px solid #ddd";
            ul.style.borderRadius = "8px";
            ul.style.boxShadow = "rgba(0, 0, 0, 0.12) 0px 1px 3px";
            ul.style.display = "none";
            ul.style.zIndex = 10;
            ul.style.minWidth = "200px";

            if (Array.isArray(subItems)) {
                subItems.forEach(sub => {
                    const subLi = document.createElement("li");
                    subLi.textContent = sub;
                    subLi.dataset.value = sub;
                    subLi.dataset.level = 'subcategory';
                    subLi.dataset.parent = name;
                    subLi.style.padding = "6px 12px";
                    subLi.style.whiteSpace = "nowrap";
                    subLi.style.cursor = "pointer";

                    // Add hover effect
                    subLi.addEventListener('mouseover', () => {
                        subLi.style.backgroundColor = '#f0f7ff';
                    });
                    subLi.addEventListener('mouseout', () => {
                        subLi.style.backgroundColor = '';
                    });

                    // Add click event for filtering
                    subLi.addEventListener('click', (e) => {
                        e.stopPropagation();
                        activeFilter = {
                            category: name,
                            subcategory: sub,
                            detailCategory: null
                        };
                        applyFilter();
                        closeAllMenus();
                    });

                    ul.appendChild(subLi);
                });
            } else {
                Object.keys(subItems).forEach(key => {
                    const innerLi = document.createElement("li");
                    innerLi.textContent = key;
                    innerLi.dataset.value = key;
                    innerLi.dataset.level = 'subcategory';
                    innerLi.dataset.parent = name;
                    innerLi.style.padding = "6px 12px";
                    innerLi.style.whiteSpace = "nowrap";
                    innerLi.style.position = "relative";
                    innerLi.style.cursor = "pointer";

                    // Add hover effect
                    innerLi.addEventListener('mouseover', () => {
                        innerLi.style.backgroundColor = '#f0f7ff';
                    });
                    innerLi.addEventListener('mouseout', () => {
                        innerLi.style.backgroundColor = '';
                    });

                    if (Array.isArray(subItems[key]) && subItems[key].length > 0) {
                        const deepUl = document.createElement("ul");
                        deepUl.className = "dropdown-menu";
                        deepUl.style.position = "absolute";
                        deepUl.style.left = "100%";
                        deepUl.style.top = "0";
                        deepUl.style.marginLeft = "5px";
                        deepUl.style.backgroundColor = "#fff";
                        deepUl.style.padding = "10px";
                        deepUl.style.border = "1px solid #ddd";
                        deepUl.style.borderRadius = "8px";
                        deepUl.style.listStyle = "none";
                        deepUl.style.display = "none";
                        deepUl.style.zIndex = 11;
                        deepUl.style.minWidth = "180px";

                        subItems[key].forEach(detail => {
                            const detailLi = document.createElement("li");
                            detailLi.textContent = detail;
                            detailLi.dataset.value = detail;
                            detailLi.dataset.level = 'detailCategory';
                            detailLi.dataset.parent = key;
                            detailLi.dataset.grandparent = name;
                            detailLi.style.padding = "6px 12px";
                            detailLi.style.whiteSpace = "nowrap";
                            detailLi.style.cursor = "pointer";

                            // Add hover effect
                            detailLi.addEventListener('mouseover', () => {
                                detailLi.style.backgroundColor = '#f0f7ff';
                            });
                            detailLi.addEventListener('mouseout', () => {
                                detailLi.style.backgroundColor = '';
                            });

                            // Add click event for filtering
                            detailLi.addEventListener('click', (e) => {
                                e.stopPropagation();
                                activeFilter = {
                                    category: name,
                                    subcategory: key,
                                    detailCategory: detail
                                };
                                applyFilter();
                                closeAllMenus();
                            });

                            deepUl.appendChild(detailLi);
                        });

                        innerLi.appendChild(deepUl);

                        innerLi.addEventListener("mouseover", (e) => {
                            e.stopPropagation();
                            // Close any previously open sub-menu
                            if (openSubMenu && openSubMenu !== deepUl) {
                                openSubMenu.style.display = "none";
                            }
                            // Show this sub-menu
                            deepUl.style.display = "block";
                            // Update reference to open sub-menu
                            openSubMenu = deepUl;
                        });
                    }

                    // Add click event for filtering when there are no detail categories
                    if (!Array.isArray(subItems[key]) || subItems[key].length === 0) {
                        innerLi.addEventListener('click', (e) => {
                            e.stopPropagation();
                            activeFilter = {
                                category: name,
                                subcategory: key,
                                detailCategory: null
                            };
                            applyFilter();
                            closeAllMenus();
                        });
                    }

                    ul.appendChild(innerLi);
                });
            }

            li.appendChild(ul);

            li.addEventListener("mouseover", (e) => {
                e.stopPropagation();
                // Close any previously open main menu
                if (openMainMenu && openMainMenu !== ul) {
                    openMainMenu.style.display = "none";
                    // Also close any open sub-menu
                    if (openSubMenu) {
                        openSubMenu.style.display = "none";
                        openSubMenu = null;
                    }
                }
                // Show this menu
                ul.style.display = "block";
                // Update reference to open main menu
                openMainMenu = ul;
            });
        }

        // Add click event for filtering at the top category level
        if (level === 'main') {
            li.addEventListener('click', (e) => {
                e.stopPropagation();
                // Only apply filter if it doesn't have sub-items or they're empty
                if (!subItems || Object.keys(subItems).length === 0) {
                    activeFilter = {
                        category: name,
                        subcategory: null,
                        detailCategory: null
                    };
                    applyFilter();
                    closeAllMenus();
                }
            });
        }

        // Add hover effect for main items
        if (level === 'main') {
            li.addEventListener('mouseover', () => {
/*                li.style.color = '#0066cc';*/
            });
            li.addEventListener('mouseout', () => {
                li.style.color = '';
            });
        }

        return li;
    }

    function loadCategories() {
        const mainCategoryList = document.getElementById("mainCategories");
        if (mainCategoryList) {
            // Clear existing items
            mainCategoryList.innerHTML = '';

            // Add "All Categories" option
            const allCategoriesItem = document.createElement("li");
            allCategoriesItem.textContent = "All Categories";
            allCategoriesItem.style.cursor = "pointer";
            allCategoriesItem.style.fontWeight = "bold";
            allCategoriesItem.addEventListener('click', () => {
                activeFilter = {
                    category: null,
                    subcategory: null,
                    detailCategory: null
                };
                applyFilter();
                closeAllMenus();
            });
            allCategoriesItem.addEventListener('mouseover', () => {
                allCategoriesItem.style.color = '#0066cc';
            });
            allCategoriesItem.addEventListener('mouseout', () => {
                allCategoriesItem.style.color = '';
            });
            mainCategoryList.appendChild(allCategoriesItem);

            // Add category items
            Object.entries(allCategories).forEach(([name, subs]) => {
                const item = createDropdownItem(name, subs);
                mainCategoryList.appendChild(item);
            });
        }
    }

    function closeAllMenus() {
        if (openMainMenu) {
            openMainMenu.style.display = "none";
            openMainMenu = null;
        }
        if (openSubMenu) {
            openSubMenu.style.display = "none";
            openSubMenu = null;
        }
    }

    function applyFilter() {
        const allItems = document.querySelectorAll('.item-card-link');
        let activeFilterText = 'All Categories';
        let noResultsFound = true;

        // Update the current filter display
        //if (activeFilter.category) {
        //    activeFilterText = activeFilter.category;
        //    if (activeFilter.subcategory) {
        //        activeFilterText += ' > ' + activeFilter.subcategory;
        //        if (activeFilter.detailCategory) {
        //            activeFilterText += ' > ' + activeFilter.detailCategory;
        //        }
        //    }
        //}

        // Update filter indicator if it exists
        //const filterIndicator = document.getElementById('current-filter');
        //if (filterIndicator) {
        //    filterIndicator.textContent = activeFilterText;
        //} else {
        //    // Create filter indicator if it doesn't exist
        //    const listingsHeader = document.querySelector('.listings h2');
        //    if (listingsHeader) {
        //        const indicator = document.createElement('span');
        //        indicator.id = 'current-filter';
        //        indicator.textContent = activeFilterText;
        //        indicator.style.fontSize = '16px';
        //        indicator.style.fontWeight = 'normal';
        //        indicator.style.color = '#555';
        //        indicator.style.marginLeft = '15px';
        //        listingsHeader.appendChild(indicator);
        //    }
        //}

        // Apply the filter
        allItems.forEach(item => {
            if (!activeFilter.category) {
                // Show all items if no filter is active
                item.style.display = 'block';
                noResultsFound = false;
                return;
            }

            // Get category data from the item
            const categoryElement = item.querySelector('.item-category');
            const subcategoryElement = item.querySelector('.item-subcategory');
            const detailCategoryElement = item.querySelector('.item-detail-category');

            // If we don't have category data in the DOM, let's extract it from the data attributes
            // or fallback to making all items visible when filtered
            let category = categoryElement ? categoryElement.textContent :
                (item.dataset.category || '');
            let subcategory = subcategoryElement ? subcategoryElement.textContent :
                (item.dataset.subcategory || '');
            let detailCategory = detailCategoryElement ? detailCategoryElement.textContent :
                (item.dataset.detailCategory || '');

            // Check if the item matches the active filter
            let matches = category.includes(activeFilter.category);

            if (matches && activeFilter.subcategory) {
                matches = subcategory.includes(activeFilter.subcategory);

                if (matches && activeFilter.detailCategory) {
                    matches = detailCategory.includes(activeFilter.detailCategory);
                }
            }

            // Show or hide the item
            if (matches) {
                item.style.display = 'block';
                noResultsFound = false;
            } else {
                item.style.display = 'none';
            }
        });

        // Show "no results" message if needed
        const noResultsMessage = document.getElementById('no-results-message');
        if (noResultsFound) {
            if (!noResultsMessage) {
                const itemsGrid = document.querySelector('.items-grid');
                if (itemsGrid) {
                    const message = document.createElement('div');
                    message.id = 'no-results-message';
                    message.style.textAlign = 'center';
                    message.style.padding = '50px';
                    message.style.color = '#666';
                    message.style.backgroundColor = '#f8f9fa';
                    message.style.borderRadius = '10px';
                    message.style.margin = '20px 0';
                    message.style.width = '100%';

                    message.innerHTML = `
                        <h3>No listings found</h3>
                        <p>No listings match the selected category criteria.</p>
                        <button id="clear-filters" style="
                            padding: 8px 16px;
                            background-color: #0066cc;
                            color: white;
                            border: none;
                            border-radius: 5px;
                            cursor: pointer;
                            margin-top: 10px;">
                            Show All Categories
                        </button>
                    `;

                    itemsGrid.appendChild(message);

                    // Add event listener to the clear filters button
                    document.getElementById('clear-filters').addEventListener('click', () => {
                        activeFilter = {
                            category: null,
                            subcategory: null,
                            detailCategory: null
                        };
                        applyFilter();
                    });
                }
            }
        } else if (noResultsMessage) {
            noResultsMessage.remove();
        }
    }

    // Add category data as data attributes to listing items
    function enhanceListingItems() {
        const allItems = document.querySelectorAll('.item-card-link');

        allItems.forEach(item => {
            // Check if we can extract category data from the DOM
            if (!item.dataset.category) {
                // Try to find category info in the item details
                const categoryText = item.querySelector('.item-category-text');
                const subcategoryText = item.querySelector('.item-subcategory-text');
                const detailCategoryText = item.querySelector('.item-detail-category-text');

                if (categoryText) {
                    item.dataset.category = categoryText.textContent.trim();
                }
                if (subcategoryText) {
                    item.dataset.subcategory = subcategoryText.textContent.trim();
                }
                if (detailCategoryText) {
                    item.dataset.detailCategory = detailCategoryText.textContent.trim();
                }

                // If we couldn't find the data, add hidden elements with the category data
                if (!item.dataset.category) {
                    // Extract from the link URL or other sources if possible
                    // For now, we'll just add the hidden elements that will be updated server-side
                    const card = item.querySelector('.item-card');
                    if (card) {
                        const info = card.querySelector('.item-info');
                        if (info) {
                            // Create hidden spans for category data that will be populated server-side
                            const categorySpan = document.createElement('span');
                            categorySpan.className = 'item-category';
                            categorySpan.style.display = 'none';

                            const subcategorySpan = document.createElement('span');
                            subcategorySpan.className = 'item-subcategory';
                            subcategorySpan.style.display = 'none';

                            const detailCategorySpan = document.createElement('span');
                            detailCategorySpan.className = 'item-detail-category';
                            detailCategorySpan.style.display = 'none';

                            info.appendChild(categorySpan);
                            info.appendChild(subcategorySpan);
                            info.appendChild(detailCategorySpan);
                        }
                    }
                }
            }
        });
    }

    // Close all menus when clicking outside
    document.addEventListener("click", () => {
        closeAllMenus();
    });

    // Initialize the categories
    loadCategories();

    // Enhance listing items with category data
    enhanceListingItems();
});