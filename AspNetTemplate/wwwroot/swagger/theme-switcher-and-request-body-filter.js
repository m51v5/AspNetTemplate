// REMOVE EMPTY VALUES FROM REQUESTS
(function () {
    var originalSwaggerUIBundle = window.SwaggerUIBundle;

    // Function to remove empty/null values
    var cleanEmpty = function (obj) {
        if (Array.isArray(obj)) {
            return obj
                .map(v => cleanEmpty(v))
                .filter(v => v !== null && v !== "" && v !== undefined);
        }
        if (obj !== null && typeof obj === 'object') {
            return Object.entries(obj).reduce((acc, [key, value]) => {
                const cleaned = cleanEmpty(value);
                if (cleaned !== null && cleaned !== "" && cleaned !== undefined) {
                    acc[key] = cleaned;
                }
                return acc;
            }, {});
        }
        return obj;
    };

    var customInterceptor = function (request) {
        // Only act on requests with body
        if (request.body) {
            var body = request.body;

            // Handle FormData
            if (body instanceof FormData) {
                var newFd = new FormData();
                for (var pair of body.entries()) {
                    var key = pair[0];
                    var val = pair[1];
                    if (val !== null && val !== "" && val !== undefined) {
                        if (val instanceof File) {
                            // Keep file if it has content or a name (some browsers send empty file with name for input type=file)
                            // If user explicitly wants to remove empty files:
                            if (val.size > 0 || val.name) {
                                newFd.append(key, val);
                            }
                        } else {
                            newFd.append(key, val);
                        }
                    }
                }
                request.body = newFd;

                // Remove Content-Type header so the browser can set it with the correct boundary
                if (request.headers && request.headers['Content-Type'] && request.headers['Content-Type'].indexOf('multipart/form-data') !== -1) {
                    delete request.headers['Content-Type'];
                }
            }
            else {
                var isString = typeof body === 'string';
                var data = body;

                if (isString) {
                    try {
                        data = JSON.parse(body);
                    } catch (e) {
                        return request;
                    }
                }

                var newData = cleanEmpty(data);
                request.body = isString ? JSON.stringify(newData) : newData;
            }
        }
        return request;
    };

    // Override the SwaggerUIBundle to inject the interceptor configuration
    window.SwaggerUIBundle = function (config) {
        var prevInterceptor = config.requestInterceptor;

        config.requestInterceptor = function (request) {
            var req = customInterceptor(request);
            if (prevInterceptor) {
                return prevInterceptor(req);
            }
            return req;
        };

        return originalSwaggerUIBundle.apply(this, arguments);
    };

    // Preserve any static properties of the original function
    for (var key in originalSwaggerUIBundle) {
        if (originalSwaggerUIBundle.hasOwnProperty(key)) {
            window.SwaggerUIBundle[key] = originalSwaggerUIBundle[key];
        }
    }

})();

// theme-switcher
document.addEventListener('DOMContentLoaded', function () {
    const switchButton = document.createElement('button');
    switchButton.innerText = 'Toggle Theme';
    switchButton.style.position = 'fixed';
    switchButton.style.top = '10px';
    switchButton.style.right = '10px';
    switchButton.style.zIndex = '9999';
    document.body.appendChild(switchButton);

    const darkThemeLink = document.querySelector('link[href="/swagger/swagger-dark.css"]');

    // Safety check in case the link isn't found
    if (darkThemeLink) {
        switchButton.addEventListener('click', function () {
            if (darkThemeLink.disabled) {
                // Enable the dark theme
                darkThemeLink.disabled = false;
            } else {
                // Disable the dark theme (revert to default light theme)
                darkThemeLink.disabled = true;
            }
        });
    }
});
