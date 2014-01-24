﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using XboxMusicLibrary.Settings;

namespace XboxMusicLibrary.Models
{
    public class Artists
    {
        public ObservableCollection<Artist> Items { get; set; }
        public string ContinuationToken { get; set; }
    }

    public class Artist
    {
        // Identifier for this piece of content. All Ids are of the form {namespace}.{actual identifier} and may be used in lookup requests.
        public string Id { get; set; }
        // The name of this piece of content.
        public string Name { get; set; }
        // A direct link to the default image associated with this piece of content.
        // See ImageUrl for details about how these links may be customized to produce different images.
        public string ImageUrl { get; set; }
        // A music.xbox.com link that redirects to a contextual page for this piece of content on the relevant Xbox Music client application depending on the user's device or operating system.
        // See Deep Link for more informatoin on how these links work and how they should be modified for revenue sharing.
        public string Link { get; set; }
        // An optional collection of other Ids that identify this piece of content on top of the main Id. Each key is the namespace or sub-namespace in which the Id belongs, and each value is a secondary Id for this piece of content.
        public Dictionary<string, string> OtherIds { get; set; }
        // An indication of the data source for this piece of content. Right now only Catalog is supported.
        public string Source { get; set; }
        // The list of musical genres associated with this album.
        public List<string> Genres { get; set; }
        public List<string> Subgenres { get; set; }
        // An optional paginated list of related artists. This list is null by default unless requested as extra information in a lookup request. 
        // Artists in this list only contain a few fields, including the Id that should be used in a lookup request in order to have the full artist properties.
        public Artists RelatedArtists { get; set; }
        // An optional paginated list of the artist's albums, ordered by decreasing order of release date (latest first). 
        // This list is null by default unless requested as extra information in a lookup request. Albums in this list only contain a few fields, including the Id that should be used in a lookup request in order to have the full album properties.
        public Albums Albums { get; set; }
        // A paginated list of the artist's top tracks, ordered by decreasing order of popularity. 
        // This list is null by default unless requested as extra information in a lookup request. Tracks in this list only contain a few fields, including the Id that should be used in a lookup request in order to have the full track properties.
        public Tracks TopTracks { get; set; }

        /// <summary>
        /// Every piece of content returned by catalog search and lookup APIs contains a field ImageUrl, which is a direct link to the content's default image, hosted on http://musicimage.xboxlive.com/. This image link generates an image with specific default properties, but it is possible to modify the link in order to change some of the image properties, such as its size. Image resolution is context-aware, so the actual image might change depending on the size parameters used. The details of the image API are shown here.
        /// You should use the image URL given in the response. The URL should not be modified or altered other than with the parameters described below. Any other use of the images will be considered as a breach of the terms of use of the API (see TBD).
        /// </summary>
        /// <param name="width">Image width. Cannot be set without height parameter.</param>
        /// <param name="height">Image height. Cannot be set without width parameter.</param>
        /// <param name="mode">Image resize mode: 
        /// - scale to resize to maximum size which fits dimension without changing the aspect ratio.
        /// - letterbox to pad to dimension after resize if aspect ratio didn't match. 
        /// - crop to get the required width and height but image is cropped. Defaults to crop if w and h are provided.</param>
        /// <param name="background">HTML-compliant color for letterbox resize mode background. Cannot be specified if mode is not set to letterbox.</param>
        /// <returns></returns>
        public string ImageUrlWithOptions(ImageSettings settings)
        {
            if (settings.Mode == ImageMode.LetterBox)
            {
                return this.ImageUrl + string.Format("&w={0}&h={1}&mode={2}&background={3}", settings.Width.ToString(), settings.Height.ToString(), settings.Mode.ToString().ToLower(), settings.Background);
            }
            else
            {
                return this.ImageUrl + string.Format("&w={0}&h={1}&mode={2}", settings.Width.ToString(), settings.Height.ToString(), settings.Mode.ToString().ToLower(), settings.Background);
            }
        }

        // Settings in order to change image format
        public ImageSettings ImageSettings { get; set; }

        public string ImageUrlEx
        {
            get
            {
                if (this.ImageSettings != null)
                {
                    return this.ImageUrlWithOptions(ImageSettings);
                }
                else
                {
                    return this.ImageUrlWithOptions(new ImageSettings(160, 160, ImageMode.Scale, "#000000"));
                }
            }
        }
    }
}

