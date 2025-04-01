/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 29.08.2016
 * Time: 20:27
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Drawing;

namespace SharedComponents.Events
{
    /// <summary>
    ///     Description of DirectEvent.
    /// </summary>
    [Serializable]
    public class DirectEvent
    {
        public DirectEvent(DirectEvents type, string message, Color? color = null, bool warning = false)
        {
            this.type = type;
            this.message = message;
            this.color = color.HasValue ? color : Color.Black;
            this.warning = warning;
        }

        public DirectEvents type { get; set; }
        public String message { get; set; }
        public Color? color { get; set; }
        public bool warning { get; set; }
    }
}