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
        }, "Others": []

    };

    let openMainMenu = null;
    let openSubMenu = null;

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

                    subLi.addEventListener('mouseover', () => {
                        subLi.style.backgroundColor = '#f0f7ff';
                    });
                    subLi.addEventListener('mouseout', () => {
                        subLi.style.backgroundColor = '';
                    });

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

                            detailLi.addEventListener('mouseover', () => {
                                detailLi.style.backgroundColor = '#f0f7ff';
                            });
                            detailLi.addEventListener('mouseout', () => {
                                detailLi.style.backgroundColor = '';
                            });

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
                            if (openSubMenu && openSubMenu !== deepUl) {
                                openSubMenu.style.display = "none";
                            }
                            deepUl.style.display = "block";
                            openSubMenu = deepUl;
                        });
                    }

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
                if (openMainMenu && openMainMenu !== ul) {
                    openMainMenu.style.display = "none";
                    if (openSubMenu) {
                        openSubMenu.style.display = "none";
                        openSubMenu = null;
                    }
                }
                ul.style.display = "block";
                openMainMenu = ul;
            });
        }

        if (level === 'main') {
            li.addEventListener('click', (e) => {
                e.stopPropagation();
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

        if (level === 'main') {
            li.addEventListener('mouseover', () => {
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
            mainCategoryList.innerHTML = '';

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


        allItems.forEach(item => {
            if (!activeFilter.category) {
                item.style.display = 'block';
                noResultsFound = false;
                return;
            }

            const categoryElement = item.querySelector('.item-category');
            const subcategoryElement = item.querySelector('.item-subcategory');
            const detailCategoryElement = item.querySelector('.item-detail-category');


            let category = categoryElement ? categoryElement.textContent :
                (item.dataset.category || '');
            let subcategory = subcategoryElement ? subcategoryElement.textContent :
                (item.dataset.subcategory || '');
            let detailCategory = detailCategoryElement ? detailCategoryElement.textContent :
                (item.dataset.detailCategory || '');

            let matches = category.includes(activeFilter.category);

            if (matches && activeFilter.subcategory) {
                matches = subcategory.includes(activeFilter.subcategory);

                if (matches && activeFilter.detailCategory) {
                    matches = detailCategory.includes(activeFilter.detailCategory);
                }
            }

            if (matches) {
                item.style.display = 'block';
                noResultsFound = false;
            } else {
                item.style.display = 'none';
            }
        });

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
                            border-radius: 50px;
                            cursor: pointer;
                            margin-top: 10px;">
                            Show All Categories
                        </button>
                    `;

                    itemsGrid.appendChild(message);

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

    function enhanceListingItems() {
        const allItems = document.querySelectorAll('.item-card-link');

        allItems.forEach(item => {
            if (!item.dataset.category) {
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

                if (!item.dataset.category) {
                    const card = item.querySelector('.item-card');
                    if (card) {
                        const info = card.querySelector('.item-info');
                        if (info) {
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

    document.addEventListener("click", () => {
        closeAllMenus();
    });

    loadCategories();

    enhanceListingItems();
});