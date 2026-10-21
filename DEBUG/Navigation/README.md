# Navigation debug and editor tools

RoutePreviewOverlay.cs and RoutePreviewJob.cs own the F3 panel, private preview navigation and displayed route samples. CheckpointDesignOverlay.cs owns editor/F3 checkpoint circles and connection drawing. Implementations moved here with existing script UIDs; maps retain their Debug/CheckpointOverlay scene nodes and authored checkpoint data. Production navigation and routing remain in WORLD/NAVIGATION and WORLD/ROUTING. No preview route mutates gameplay routes.
