﻿
_gfxRendererVars = {
	'tempImg': pygame.Surface((10, 10)),
	'events': [],
	'eventsLength': 0,
	'images': [],
}

def gfxRendererSetData(events, eventsLength, images):
	_gfxRendererVars['events'] = events
	_gfxRendererVars['eventsLength'] = eventsLength
	_gfxRendererVars['images'] = images

def gfxRender():
	screen = _global_vars['virtual_screen']
	tempImg = _gfxRendererVars['tempImg']
	events = _gfxRendererVars['events']
	eventsLength = _gfxRendererVars['eventsLength']
	images = _gfxRendererVars['images']
	tempImgWidth, tempImgHeight = tempImg.get_size()
	screenWidth, screenHeight = screen.get_size()

	imageIndex = 0
	i = 0
	while i < eventsLength:
		command = events[i]

		if command == 5:
			mask = events[i | 1]
			image = images[imageIndex][0][3]
			imageIndex += 1
			x = events[i | 8]
			y = events[i | 9]
			if mask == 0:
				# basic
				screen.blit(image, (x, y))
			elif mask == 1:
				# slice
				sx, sy, sw, sh = events[i | 2], events[i | 3], events[i | 4], events[i | 5]
				screen.blit(image, (x, y), (sx, sy, sw, sh))
			elif mask == 2:
				# stretch
				screen.blit(image, (x, y, events[i | 6], events[i | 7]))
			elif mask == 3:
				# slice and stretch
				sx, sy, sw, sh = events[i | 2], events[i | 3], events[i | 4], events[i | 5]
				screen.blit(image, (x, y, events[i | 6], events[i | 7]), (sx, sy, sw, sh))
			else:

				alpha = 255
				if (mask & 8) != 0:
					alpha = events[i | 11]
				
				if alpha > 0:
					
					w, h = image.get_size()
					if (mask & 1) != 0:
						sx, sy, sw, sh = events[i | 2], events[i | 3], events[i | 4], events[i | 5]
					else:
						sx, sy, sw, sh = 0, 0, w, h

					if (mask & 2) != 0:
						tw, th = events[i | 6], events[i | 7]
					else:
						tw, th = w, h

					if (mask & 4) != 0:
						angle = events[i | 10] / 1048576.0 * -180 / math.pi
					else:
						angle = None
					
					imgToBlit = None
					if angle != None:
						if mask == 4: # just rotation, not other transforms
							rotated = pygame.transform.rotate(image, angle)
							w, h = rotated.get_size()
							screen.blit(rotated, (x - w // 2, y - h // 2))
						else:
							rotatedTempImg = pygame.Surface((tw, th), pygame.SRCALPHA)
							rotatedTempImg.blit(image, (0, 0, tw, th), (sx, sy, sw, sh))
							rotated = pygame.transform.rotate(rotatedTempImg, angle)
							x -= rotated.get_width() // 2
							y -= rotated.get_height() // 2
							if alpha < 255:
								imgToBlit = rotated
							else:
								screen.blit(rotated, (x, y))
					else:
						imgToBlit = pygame.Surface((tw, th), pygame.SRCALPHA)
						imgToBlit.blit(image, (0, 0, tw, th), (sx, sy, sw, sh))

					if imgToBlit != None:
						w, h = imgToBlit.get_size()
						
						if w > tempImgWidth or h > tempImgHeight:
							tempImgWidth = max(tempImgWidth, w)
							tempImgHeight = max(tempImgHeight, h)
							tempImg = pygame.Surface((tempImgWidth, tempImgHeight)) # intentionally no alpha channel
						tempImg.blit(screen, (-x, -y, w, h))
						tempImg.blit(imgToBlit, (0, 0))
						tempImg.set_alpha(alpha)
						screen.blit(tempImg, (x, y, w, h), (0, 0, w, h))
		elif command < 3:
			alpha = events[i | 8]
			color = events[i | 5 : i | 8]
			area = events[i | 1: i | 5]
			if alpha == 255:
				if command == 1:
					if area[3] == 1:
						# PyGame has a bug where rectangles of height 1 do not get rendered.
						pygame.draw.line(screen, color, (area[0], area[1]), (area[0] + area[2], area[1]))
					else:
						pygame.draw.rect(screen, color, area)
				elif command == 2:
					pygame.draw.ellipse(screen, color, area)
			elif alpha > 0:
				wh = area[2:]
				t = pygame.Surface(wh) # intenionally no alpha channel
				t.blit(screen, (-area[0], -area[1]))
				if command == 1:
					t.fill(color)
				else:
					pygame.draw.ellipse(t, color, (0, 0, w[0], h[0]))
				t.set_alpha(alpha)
				screen.blit(t, area[:2])
		elif command == 4:
			# triangle
			ax = events[i | 1]
			ay = events[i | 2]
			bx = events[i | 3]
			by = events[i | 4]
			cx = events[i | 5]
			cy = events[i | 6]
			red = events[i | 7]
			green = events[i | 8]
			blue = events[i | 9]
			alpha = events[i | 10]
			if alpha == 255:
				pygame.draw.polygon(screen, (red, green, blue, alpha), [(ax, ay), (bx, by), (cx, cy)])
			else:
				minX = ax
				minY = ay
				maxX = ax
				maxY = ay
				if bx < minX: minX = bx
				if cx < minX: minX = cx
				if by < minY: minY = by
				if cy < minY: minY = cy
				if bx > maxX: maxX = bx
				if cx > maxX: maxX = cx
				if by > maxY: maxY = by
				if cy > maxY: maxY = cy
				width = maxX - minX
				height = maxY - minY
				if width > tempImgWidth or height > tempImgHeight:
					tempImgWidth = max(tempImgWidth, width)
					tempImgHeight = max(tempImgHeight, height)
					tempImg = pygame.Surface((tempImgWidth, tempImgHeight)) # intentionally no alpha channel
				tempImg.blit(screen, (0, 0, width, height), (minX, minY, width, height))
				pygame.draw.polygon(tempImg, (red, green, blue, alpha), [(ax - minX, ay - minY), (bx - minX, by - minY), (cx - minX, cy - minY)])
				tempImg.set_alpha(alpha)
				screen.blit(tempImg, (minX, minY, width, height), (0, 0, width, height))

		else: # command == 3 is line
			pass
		i += 16
	
	_gfxRendererVars['tempImg'] = tempImg
