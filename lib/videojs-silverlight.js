/*! videojs-silverlight - v0.0.0 - 2015-1-6
 * Copyright (c) 2015 Afterster
 * Licensed under the Apache-2.0 license. */
(function(window, videojs) {
  'use strict';

  var defaults = {
        option: true
      },
      silverlight;

  /**
   * Initialize the plugin.
   * @param options (optional) {object} configuration for the plugin
   */
  silverlight = function(options) {
    var settings = videojs.util.mergeOptions(defaults, options),
        player = this;

    // TODO: write some amazing plugin code
  };

  // register the plugin
  videojs.plugin('silverlight', silverlight);
})(window, window.videojs);
