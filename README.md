# Video.js Silverlight

Video.js Silverlight Tech plug-in

A Video.js tech plugin that add WMV, WMA, MP4 (AAC / H264 codecs), MP3, WAV (PCM) and FLAC (no seeking) stream support through Silverlight.

## Getting Started

Once you've added the plugin script to your page, you can use it with any supported video:
 * Include JavaScript files
```html
<script src="video.js"></script>
<script src="videojs-silverlight.js"></script>
```
 * And add this new tech to the player:
```html
data-setup='{ "techOrder": ["silverlight"] }'
```

There's also a [working example](example.html) of the plugin you can check out if you're having trouble.

## Documentation
### Plugin Options

This plugin has a global configuration to setup XAP file location.
```html
<script>
    videojs.options.silverlight.xap = "video-js.xap";
</script>
```

## Release History

 - 0.2.0: Wav and Flac support
 - 0.1.0: Initial release
